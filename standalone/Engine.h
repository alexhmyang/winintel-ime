// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
// Project: winintel AI Input Method - https://github.com/alexhmyang/winintel-ime
#pragma once
#ifndef NOMINMAX
#define NOMINMAX
#endif
#ifdef _WIN32
#include <windows.h>
#else
#include "../portable/Platform.h"
#endif
#include <string>
#include <vector>
#include <algorithm>
#include <unordered_map>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <cctype>
#include <cstdint>
#include <cwctype>
#include <functional>
#include <unordered_set>
struct Candidate { std::wstring text; float score; };
#include "NameCandidates.h"
#include "IndustryLexicon.h"
class Engine {
  NameCandidates names;
  IndustryLexicon industry;
  struct Entry { unsigned code, text; float score; };
  HANDLE file=INVALID_HANDLE_VALUE, mapping=nullptr;
  const char* base=nullptr;
  const Entry* entries=nullptr;
  const char* strings=nullptr;
  unsigned count=0;
  std::unordered_map<std::wstring,size_t> candidateOffsets;
  std::unordered_map<std::string,std::vector<Candidate>> segmentCache;
  struct Choice { unsigned count=0; uint64_t sequence=0; };
  std::unordered_map<std::string,Choice> learning;
  std::wstring learnPath,legacyPath,mutexName;
  std::wstring personalPath,apiPath,hotPath,builtinHotPath;
  std::unordered_map<std::string,std::vector<std::wstring>> hot,builtinHot;
  std::unordered_map<wchar_t,std::vector<std::string>> readings;
  std::unordered_map<std::string,std::vector<std::wstring>> personal;
  std::unordered_map<std::string,std::vector<std::wstring>> api;
  std::unordered_map<std::wstring,uint64_t> phraseStamps;
  void loadReadings(const std::wstring& path){
    FILE* f=nullptr;_wfopen_s(&f,path.c_str(),L"rb");if(!f)return;char line[1024];
    while(fgets(line,sizeof(line),f)){
      std::string row=line;auto tab=row.find('\t');if(tab==std::string::npos)continue;
      auto text=wide(row.substr(0,tab).c_str());if(text.size()!=1)continue;
      size_t at=tab+1;
      while(at<row.size()){
        size_t end=row.find_first_of(" \r\n",at);if(end==std::string::npos)end=row.size();
        if(end>at)readings[text[0]].push_back(row.substr(at,end-at));at=end+1;
      }
    }fclose(f);
  }
  std::string initials(const std::string& code,const std::wstring& text) const{
    if(text.size()<2||text.size()>48)return "";
    std::unordered_set<size_t> failed;
    std::function<bool(size_t,size_t,std::string&)> match=[&](size_t ch,size_t at,std::string& result){
      if(ch==text.size())return at==code.size();
      size_t state=ch*(code.size()+1)+at;if(failed.count(state))return false;
      auto found=readings.find(text[ch]);if(found==readings.end())return false;
      for(auto& syllable:found->second)if(code.compare(at,syllable.size(),syllable)==0){
        result.push_back(syllable[0]);
        if(match(ch+1,at+syllable.size(),result))return true;
        result.pop_back();
      }failed.insert(state);return false;
    };
    std::string result;return match(0,0,result)?result:"";
  }
  bool fullSpelling(const std::string& code,const std::wstring& text) const{
    if(text.size()==1){
      auto found=readings.find(text[0]);
      return found!=readings.end()&&std::find(found->second.begin(),found->second.end(),code)!=found->second.end();
    }
    return !initials(code,text).empty();
  }
  void loadPhrases(const std::wstring& path,std::unordered_map<std::string,std::vector<std::wstring>>& phrases){
    WIN32_FILE_ATTRIBUTE_DATA info={};
    if(!GetFileAttributesExW(path.c_str(),GetFileExInfoStandard,&info)){
      if(GetLastError()==ERROR_FILE_NOT_FOUND){phrases.clear();phraseStamps.erase(path);}return;
    }
    uint64_t stamp=((uint64_t)info.ftLastWriteTime.dwHighDateTime<<32)|info.ftLastWriteTime.dwLowDateTime;
    auto previous=phraseStamps.find(path);if(previous!=phraseStamps.end()&&previous->second==stamp)return;
    FILE* f=nullptr;_wfopen_s(&f,path.c_str(),L"rb");if(!f)return;phrases.clear();char line[4096];unsigned loaded=0;
    while(fgets(line,sizeof(line),f)&&loaded++<10000){
      std::string row=line;while(!row.empty()&&(row.back()=='\r'||row.back()=='\n'))row.pop_back();
      auto tab=row.find('\t');if(tab==std::string::npos||!tab||tab>256)continue;
      auto code=normalize(row.substr(0,tab));auto text=wide(row.substr(tab+1).c_str());
      if(text.empty()||text.size()>48||code.find_first_not_of("abcdefghijklmnopqrstuvwxyz")!=std::string::npos)continue;
      phrases[code].push_back(text);auto abbr=initials(code,text);
      if(!abbr.empty()&&abbr!=code)phrases[abbr].push_back(text);
    }fclose(f);phraseStamps[path]=stamp;
  }
  uint64_t sequence=0;
  struct LearningLock {
    HANDLE handle=nullptr;bool acquired=false;
    explicit LearningLock(const std::wstring& name){
      if(name.empty())return;
      handle=CreateMutexW(nullptr,FALSE,name.c_str());
      if(handle){DWORD result=WaitForSingleObject(handle,1000);acquired=result==WAIT_OBJECT_0||result==WAIT_ABANDONED;}
    }
    ~LearningLock(){if(acquired)ReleaseMutex(handle);if(handle)CloseHandle(handle);}
  };
  static std::string normalize(std::string code){for(char& c:code)c=(char)tolower((unsigned char)c);return code;}
  static std::string utf8(const std::wstring& text){
    int n=WideCharToMultiByte(CP_UTF8,0,text.data(),(int)text.size(),nullptr,0,nullptr,nullptr);
    std::string result(n,0);if(n)WideCharToMultiByte(CP_UTF8,0,text.data(),(int)text.size(),&result[0],n,nullptr,nullptr);
    return result;
  }
  void loadLearning(){
    FILE* f=nullptr;_wfopen_s(&f,learnPath.c_str(),L"rb");bool legacy=false;
    if(!f){_wfopen_s(&f,legacyPath.c_str(),L"rb");legacy=true;}
    if(!f)return;
    learning.clear();sequence=0;char line[4096];
    while(fgets(line,sizeof(line),f)){
      std::string row=line;while(!row.empty()&&(row.back()=='\n'||row.back()=='\r'))row.pop_back();
      auto last=row.rfind('\t');if(last==std::string::npos)continue;
      Choice choice;
      if(legacy){choice.count=std::min(1000u,(unsigned)strtoul(row.c_str()+last+1,nullptr,10));}
      else{
        choice.sequence=_strtoui64(row.c_str()+last+1,nullptr,10);row.resize(last);last=row.rfind('\t');
        if(last==std::string::npos)continue;
        choice.count=std::min(1000u,(unsigned)strtoul(row.c_str()+last+1,nullptr,10));
      }
      row.resize(last);auto separator=row.find('\t');
      if(separator==std::string::npos||separator==0||separator>256||separator+1==row.size()||!choice.count)continue;
      learning[normalize(row.substr(0,separator))+row.substr(separator)]=choice;sequence=std::max(sequence,choice.sequence);
      if(learning.size()>=10000)break;
    }
    fclose(f);
  }
  void refreshLearning(){LearningLock lock(mutexName);if(lock.acquired)loadLearning();}
  bool saveLearning(){
    if(learnPath.empty())return false;
    auto temporary=learnPath+L"."+std::to_wstring(GetCurrentProcessId())+L".tmp";
    FILE* f=nullptr;_wfopen_s(&f,temporary.c_str(),L"wb");if(!f)return false;
    bool ok=true;
    for(auto& item:learning)if(fprintf(f,"%s\t%u\t%llu\n",item.first.c_str(),item.second.count,(unsigned long long)item.second.sequence)<0)ok=false;
    if(fclose(f))ok=false;
    if(ok)ok=MoveFileExW(temporary.c_str(),learnPath.c_str(),MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH)!=FALSE;
    if(!ok)DeleteFileW(temporary.c_str());return ok;
  }
  static std::wstring wide(const char* s) {
    int n=MultiByteToWideChar(CP_UTF8,0,s,-1,nullptr,0);
    if(n<1) return L"";
    std::wstring out(n,L'\0'); MultiByteToWideChar(CP_UTF8,0,s,-1,&out[0],n); out.pop_back(); return out;
  }
  unsigned lower(const std::string& code) const {
    unsigned lo=0,hi=count;
    while(lo<hi) {unsigned m=lo+(hi-lo)/2;if(strcmp(strings+entries[m].code,code.c_str())<0)lo=m+1;else hi=m;} return lo;
  }
  void collect(const std::string& key,float penalty,std::vector<Candidate>& result,unsigned limit=0) {
    unsigned at=lower(key);
    unsigned scanned=0;
    while(at<count && key==strings+entries[at].code && (!limit||scanned++<limit)) {
      auto text=wide(strings+entries[at].text);
      float score=entries[at].score-penalty;
      auto prior=candidateOffsets.find(text);
      if(prior==candidateOffsets.end()){candidateOffsets[text]=result.size();result.push_back({text,score});}
      else result[prior->second].score=std::max(result[prior->second].score,score);
      at++;
    }
  }
public:
  ~Engine(){if(base)UnmapViewOfFile(base);if(mapping)CloseHandle(mapping);if(file!=INVALID_HANDLE_VALUE)CloseHandle(file);}
  bool open(const std::wstring& path) {
    if(base)return true;
    file=CreateFileW(path.c_str(),GENERIC_READ,FILE_SHARE_READ,nullptr,OPEN_EXISTING,0,nullptr);
    if(file==INVALID_HANDLE_VALUE)return false;
    LARGE_INTEGER size; if(!GetFileSizeEx(file,&size)||size.QuadPart<16)return false;
    mapping=CreateFileMappingW(file,nullptr,PAGE_READONLY,0,0,nullptr);if(!mapping)return false;
    base=(const char*)MapViewOfFile(mapping,FILE_MAP_READ,0,0,0);if(!base)return false;
    if(memcmp(base,"WIME0002",8))return false;
    count=*(unsigned*)(base+8); unsigned start=*(unsigned*)(base+12);
    if(start!=16ULL+count*12 || start>=size.QuadPart){count=0;return false;}
    entries=(const Entry*)(base+16); strings=base+start;
    auto slash=path.find_last_of(L"\\/");loadReadings(path.substr(0,slash+1)+L"character-readings.tsv");
    names.load(path.substr(0,slash+1)+L"name-parts.tsv");
    industry.open(path.substr(0,slash));
    builtinHotPath=path.substr(0,slash+1)+L"hot-chinese.tsv";
    // Validate all offsets once, before using the mapped dictionary.
    size_t length=(size_t)size.QuadPart-start;
    for(unsigned i=0;i<count;i++)if(entries[i].code>=length||entries[i].text>=length||!memchr(strings+entries[i].code,0,std::min<size_t>(65,length-entries[i].code))||!memchr(strings+entries[i].text,0,std::min<size_t>(193,length-entries[i].text))){count=0;return false;}
    wchar_t app[MAX_PATH];if(GetEnvironmentVariableW(L"LOCALAPPDATA",app,MAX_PATH)){
      std::wstring folder=std::wstring(app)+L"\\winintel-ime-native";CreateDirectoryW(folder.c_str(),nullptr);
      learnPath=folder+L"\\choices-v2.tsv";legacyPath=folder+L"\\choices.tsv";
      personalPath=folder+L"\\personal-phrases.tsv";
      apiPath=folder+L"\\api-phrases.tsv";
      hotPath=folder+L"\\hot-chinese.tsv";
      uint64_t hash=1469598103934665603ULL;for(wchar_t ch:learnPath){hash^=(uint64_t)towlower(ch);hash*=1099511628211ULL;}
      mutexName=L"Local\\winintel-choice-"+std::to_wstring(hash);refreshLearning();
    }
    return true;
  }
  std::vector<Candidate> query(std::string code,bool expand=true) {
    std::vector<Candidate> result;if(!count||code.empty())return result;
    candidateOffsets.clear();
    code=normalize(code);refreshLearning();loadPhrases(personalPath,personal);loadPhrases(apiPath,api);loadPhrases(hotPath,hot);loadPhrases(builtinHotPath,builtinHot);
    collect(code,0,result);
    const size_t exactCount=result.size();
    std::unordered_set<std::wstring> exactSpelled;
    for(const auto& c:result)if(fullSpelling(code,c.text))exactSpelled.insert(c.text);
    if(code.size()==1){
      std::unordered_set<std::string> syllables;
      for(auto& r:readings)for(auto& s:r.second)if(s.compare(0,1,code)==0)syllables.insert(s);
      for(auto& s:syllables)collect(s,9,result,32);
    }
    if(code.size()>=1){
      unsigned at=lower(code),scanned=0;
      while(at<count && !strncmp(strings+entries[at].code,code.c_str(),code.size()) && scanned++<128){
        std::string key=strings+entries[at].code;
        if(key!=code)collect(key,9+(float)std::min<size_t>(6,key.size()-code.size()),result,24);
        do{at++;}while(at<count&&key==strings+entries[at].code);
      }
    }
    std::vector<std::string> corrections;
    auto corrected=[&](const std::string& key,float penalty){
      auto at=lower(key);if(at<count&&key==strings+entries[at].code)corrections.push_back(key);
      collect(key,penalty+(exactCount>=9?100:0),result);
    };
    if(exactCount<50 && code.size()>=3 && code.size()<=24){
      for(size_t i=0;i<code.size();i++){
        auto s=code;s.erase(i,1);corrected(s,13);
        if(i+1<code.size()){s=code;std::swap(s[i],s[i+1]);corrected(s,12);}
        for(char c='a';c<='z';c++){
          if(c!=code[i]){s=code;s[i]=c;corrected(s,14);}
          s=code;s.insert(i,1,c);corrected(s,13);
        }
      }
      for(char c='a';c<='z';c++)corrected(code+c,13);
    }
    std::unordered_set<std::wstring> legacySpelled;
    // Retain established phrase paths while expanding sparse candidate sets.
    if(exactCount<50 && code.size()>=4 && code.size()<=256){
      const size_t beam=128;
      std::vector<std::vector<Candidate>> dp(code.size()+1);dp[0].push_back({L"",0});
      std::vector<std::vector<Candidate>> legacy(code.size()+1);legacy[0].push_back({L"",0});
      for(size_t end=1;end<=code.size();end++){
        std::unordered_map<std::wstring,float> paths;
        for(size_t start=end>24?end-24:0;start<end;start++){
        if(dp[start].empty())continue;
        auto spelling=code.substr(start,end-start);auto cached=segmentCache.find(spelling);
        if(cached==segmentCache.end()){
          std::vector<Candidate> choices;
          for(unsigned at=lower(spelling);at<count&&spelling==strings+entries[at].code;at++){
            auto text=wide(strings+entries[at].text);
            if(text.empty()||spelling.size()<text.size()*2)continue;
            if(std::any_of(text.begin(),text.end(),[](wchar_t ch){return ch<0x4e00||ch>0x9fff;}))continue;
            // Do not splice initial-only abbreviations into full-pinyin sentences.
            if(!fullSpelling(spelling,text))continue;
            choices.push_back({std::move(text),entries[at].score});
          }
          std::sort(choices.begin(),choices.end(),[](const auto& a,const auto& b){return a.score!=b.score?a.score>b.score:a.text<b.text;});
          if(choices.size()>32)choices.resize(32);
          if(segmentCache.size()>4096)segmentCache.clear();
          cached=segmentCache.emplace(spelling,std::move(choices)).first;
        }
        for(const auto& before:dp[start])for(const auto& c:cached->second){
          auto text=before.text+c.text;
          float score=before.score+c.score*(float)c.text.size()-50;
          auto old=paths.find(text);if(old==paths.end()||score>old->second)paths[text]=score;
        }
        for(const auto& before:legacy[start])for(size_t i=0;i<std::min<size_t>(4,cached->second.size());i++){
          const auto& c=cached->second[i];legacy[end].push_back({before.text+c.text,before.score+c.score-34-(c.text.size()==1?5:0)});
        }
        }
        std::sort(legacy[end].begin(),legacy[end].end(),[](const auto& a,const auto& b){return a.score!=b.score?a.score>b.score:a.text<b.text;});
        if(legacy[end].size()>8)legacy[end].resize(8);
        for(auto& p:paths)dp[end].push_back({p.first,p.second});
        std::sort(dp[end].begin(),dp[end].end(),[](const auto& a,const auto& b){if(a.text.size()!=b.text.size())return a.text.size()<b.text.size();return a.score!=b.score?a.score>b.score:a.text<b.text;});
        if(dp[end].size()>beam)dp[end].resize(beam);
      }
      for(auto c:legacy.back())if(fullSpelling(code,c.text)){
        legacySpelled.insert(c.text);
        if(exactCount>0)c.score-=100;
        auto found=candidateOffsets.find(c.text);
        if(found==candidateOffsets.end()){candidateOffsets[c.text]=result.size();result.push_back(c);}
        else result[found->second].score=std::max(result[found->second].score,c.score);
      }
      for(auto& c:dp.back()){
        c.score=c.score/(float)c.text.size()-100;
        auto prior=candidateOffsets.find(c.text);
        if(prior==candidateOffsets.end()){candidateOffsets[c.text]=result.size();result.push_back(c);}
        else result[prior->second].score=std::max(result[prior->second].score,c.score);
      }
    }
    if(expand&&result.size()<50){
      std::unordered_set<std::wstring> seen;for(auto& c:result)seen.insert(c.text);
      std::sort(corrections.begin(),corrections.end());corrections.erase(std::unique(corrections.begin(),corrections.end()),corrections.end());
      for(auto& key:corrections){
        if(key==code)continue;
        for(auto c:query(key,false))if(seen.insert(c.text).second){c.score-=35;result.push_back(c);}
        if(result.size()>=50)break;
      }
    }
    for(auto& text:names.query(code)){
      const auto full=names.spelling(text).first;float prior=names.prior(text);
      if(full!=code||prior<=0)continue;
      auto it=std::find_if(result.begin(),result.end(),[&](const auto& c){return c.text==text;});
      float score=32+prior;
      if(it==result.end())result.push_back({text,score});else it->score=std::max(it->score,score);
    }
    // Restore learned candidates even when prefix scanning no longer includes them.
    auto prefix=code+"\t";
    auto custom=personal.find(code);
    auto remote=api.find(code);
    for(auto* pack:{&hot,&builtinHot}){auto match=pack->find(code);if(match!=pack->end())for(auto& text:match->second)
      if(std::none_of(result.begin(),result.end(),[&](const Candidate& c){return c.text==text;}))result.push_back({text,110});}
    if(remote!=api.end())for(auto& text:remote->second)
      if(std::none_of(result.begin(),result.end(),[&](const Candidate& c){return c.text==text;}))result.push_back({text,40});
    if(custom!=personal.end())for(auto& text:custom->second)
      if(std::none_of(result.begin(),result.end(),[&](const Candidate& c){return c.text==text;}))result.push_back({text,100});
    for(auto& item:learning)if(item.first.compare(0,prefix.size(),prefix)==0){
      auto text=wide(item.first.c_str()+prefix.size());
      if(std::none_of(result.begin(),result.end(),[&](const Candidate& c){return c.text==text;}))result.push_back({text,0});
    }
    industry.appendChinese(code,result);
    if(expand&&result.size()<50){
      auto seeds=result;
      std::unordered_set<std::wstring> seen;for(auto& c:result)seen.insert(c.text);
      for(auto& seed:seeds){
        std::string full;for(auto ch:seed.text){
          auto r=readings.find(ch);if(r==readings.end()||r->second.empty()){full.clear();break;}full+=r->second.front();
        }
        if(full.empty()||full==code)continue;
        float score=-180;
        for(auto c:query(full,false))if(seen.insert(c.text).second){c.score=score;score-=.05f;result.push_back(c);}
        if(result.size()>=50)break;
      }
      for(size_t length=code.size()>2?code.size()-1:0;length>=2&&result.size()<50;length--){
        float score=-220;
        for(auto c:query(code.substr(0,length),false))if(seen.insert(c.text).second){c.score=score;score-=.05f;result.push_back(c);}
      }
    }
    // A short, valid full-pinyin phrase must precede unrelated typo suggestions.
    // Expanded paths remain below established dictionary, name and correction scores.
    std::unordered_set<std::wstring> preferred;
    if(exactSpelled.empty()&&code.size()>=6)for(const auto& c:result)
      if(c.text.size()==2&&legacySpelled.count(c.text))preferred.insert(c.text);
    std::sort(result.begin(),result.end(),[&](const Candidate&a,const Candidate&b){
      auto ai=learning.find(prefix+utf8(a.text)),bi=learning.find(prefix+utf8(b.text));
      bool al=ai!=learning.end(),bl=bi!=learning.end();
      if(al!=bl)return al;
      if(al){if(ai->second.sequence!=bi->second.sequence)return ai->second.sequence>bi->second.sequence;
        if(ai->second.count!=bi->second.count)return ai->second.count>bi->second.count;}
      bool ap=custom!=personal.end()&&std::find(custom->second.begin(),custom->second.end(),a.text)!=custom->second.end();
      bool bp=custom!=personal.end()&&std::find(custom->second.begin(),custom->second.end(),b.text)!=custom->second.end();
      if(ap!=bp)return ap;
      bool ar=remote!=api.end()&&std::find(remote->second.begin(),remote->second.end(),a.text)!=remote->second.end();
      bool br=remote!=api.end()&&std::find(remote->second.begin(),remote->second.end(),b.text)!=remote->second.end();
      if(ar!=br)return ar;
      bool af=preferred.count(a.text)>0,bf=preferred.count(b.text)>0;
      if(af!=bf)return af;
      return a.score!=b.score?a.score>b.score:a.text<b.text;
    });
    // Keep normal words and learned choices ahead of speculative names; reserve
    // three visible slots, then retain the rest for ordinary candidate paging.
    const size_t nameSlots=code.size()==3?6:3;
    size_t insertAt=std::min<size_t>(nameSlots==6?3:5,result.size()),visible=0;
    if(hasPreference(code)||custom!=personal.end())insertAt=std::min<size_t>(5,result.size());
    for(auto& text:names.query(code)){
      if(std::any_of(result.begin(),result.end(),[&](const Candidate& c){return c.text==text;}))continue;
      Candidate row{text,-100};
      if(visible++<nameSlots)result.insert(result.begin()+insertAt++,row);else result.push_back(row);
    }
    if(result.empty())result.push_back({std::wstring(code.begin(),code.end()),0});
    return result;
  }
  void learn(const std::string& code,const std::wstring& text){
    auto normalized=normalize(code),utf=utf8(text);
    if(normalized.empty()||normalized.size()>256||utf.empty()||utf.size()>3072||normalized.find_first_of("\t\r\n")!=std::string::npos||utf.find_first_of("\t\r\n")!=std::string::npos)return;
    LearningLock lock(mutexName);if(!lock.acquired)return;loadLearning();
    auto& value=learning[normalized+"\t"+utf];value.count=std::min(1000u,value.count+1);value.sequence=++sequence;
    auto abbr=initials(normalized,text);
    std::string full;
    if(abbr.empty()&&text.size()>=2&&text.size()<=48){
      auto name=names.spelling(text);full=name.first;abbr=name.second;
      if(full.empty())for(auto ch:text){auto found=readings.find(ch);if(found==readings.end()||found->second.empty()){full.clear();abbr.clear();break;}
        full+=found->second.front();abbr+=found->second.front()[0];}
    }
    if(!abbr.empty()&&abbr!=normalized){
      auto& alias=learning[abbr+"\t"+utf];alias.count=std::min(1000u,alias.count+1);alias.sequence=value.sequence;
    }
    if(!full.empty()&&full!=normalized){
      auto& alias=learning[full+"\t"+utf];alias.count=std::min(1000u,alias.count+1);alias.sequence=value.sequence;
    }
    if(learning.size()>10000){
      auto oldest=std::min_element(learning.begin(),learning.end(),[](const auto& a,const auto& b){
        return a.second.sequence!=b.second.sequence?a.second.sequence<b.second.sequence:a.first<b.first;
      });learning.erase(oldest);
    }
    saveLearning();
  }
  bool hasPreference(const std::string& code) const{
    auto prefix=normalize(code)+"\t";
    for(auto& item:learning)if(item.first.compare(0,prefix.size(),prefix)==0)return true;
    return false;
  }
  void setIndustry(const std::string& profile){industry.setProfile(profile);}
};
