// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
#pragma once
#include "Engine.h"
class EnglishLexicon {
  struct Word {std::string key;std::wstring text;float weight;};
  std::vector<Word> words;
  std::wstring updates;ULONGLONG checked=0;FILETIME stamp={};
  size_t baseline=0;
  void load(const std::wstring& path) {
    FILE* f=nullptr;_wfopen_s(&f,path.c_str(),L"rb");if(!f)return;
    char line[2048];unsigned count=0;
    while(fgets(line,sizeof(line),f)&&count++<50000){
      std::string row=line;auto tab=row.find('\t');if(tab==std::string::npos)continue;
      auto key=row.substr(0,tab);for(char& c:key)c=(char)tolower((unsigned char)c);
      auto end=row.find_first_of("\t\r\n",tab+1);auto text=row.substr(tab+1,end-tab-1);
      if(key.empty()||key.size()>64||key.find_first_not_of("abcdefghijklmnopqrstuvwxyz")!=std::string::npos||text.empty())continue;
      int n=MultiByteToWideChar(CP_UTF8,MB_ERR_INVALID_CHARS,text.data(),(int)text.size(),nullptr,0);if(n<=0||n>96)continue;
      std::wstring wide(n,0);MultiByteToWideChar(CP_UTF8,MB_ERR_INVALID_CHARS,text.data(),(int)text.size(),&wide[0],n);
      float weight=end!=std::string::npos&&row[end]=='\t'?(float)atof(row.c_str()+end+1):0;
      words.push_back({key,wide,weight});
    }fclose(f);
  }
  void sort(){std::sort(words.begin(),words.end(),[](const Word&a,const Word&b){return a.key<b.key;});}
public:
  void open(const std::wstring& root){
    load(root+L"\\english-words.tsv");baseline=words.size();
    wchar_t app[MAX_PATH];if(GetEnvironmentVariableW(L"LOCALAPPDATA",app,MAX_PATH))updates=std::wstring(app)+L"\\winintel-ime-native\\hot-english.tsv";
    // Keep the baseline unsorted until the first update check has appended data.
    sort();baseline=words.size();
  }
  std::vector<Candidate> query(const std::string& typed){
    if(GetTickCount64()-checked>2000){checked=GetTickCount64();WIN32_FILE_ATTRIBUTE_DATA info={};
      if(GetFileAttributesExW(updates.c_str(),GetFileExInfoStandard,&info)&&CompareFileTime(&stamp,&info.ftLastWriteTime)!=0){
        // Rebuild from the baseline, so removed remote words disappear too.
        words.erase(std::remove_if(words.begin(),words.end(),[](const Word& w){return w.weight>=10000;}),words.end());
        size_t start=words.size();load(updates);for(size_t i=start;i<words.size();i++)words[i].weight+=10000;
        stamp=info.ftLastWriteTime;sort();
      }
    }
    std::string key=typed;for(char& c:key)c=(char)tolower((unsigned char)c);
    std::vector<Candidate> result;if(key.empty())return result;
    auto it=std::lower_bound(words.begin(),words.end(),key,[](const Word&w,const std::string&k){return w.key<k;});
    unsigned scanned=0;bool upper=typed.size()>1&&typed.find_first_of("abcdefghijklmnopqrstuvwxyz")==std::string::npos;
    for(;it!=words.end()&&it->key.compare(0,key.size(),key)==0&&scanned++<512;++it){
      auto text=it->text;
      if(upper)for(wchar_t& c:text)c=towupper(c);
      else if(isupper((unsigned char)typed[0])&&!text.empty())text[0]=towupper(text[0]);
      float score=(it->key==key?100000.f:0.f)+it->weight-(float)(it->key.size()-key.size());
      auto found=std::find_if(result.begin(),result.end(),[&](const Candidate&c){return c.text==text;});
      if(found==result.end())result.push_back({text,score});else found->score=std::max(found->score,score);
    }
    std::sort(result.begin(),result.end(),[](const Candidate&a,const Candidate&b){return a.score!=b.score?a.score>b.score:a.text<b.text;});
    if(result.size()>36)result.resize(36);
    if(result.empty())result.push_back({std::wstring(typed.begin(),typed.end()),0});
    return result;
  }
};
