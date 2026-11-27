// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
#pragma once
#include <sstream>
class NameCandidates {
  struct Part{std::wstring text;std::vector<std::string> syllables;float rank;};
  std::vector<Part> surnames,given;
  std::unordered_map<std::string,std::vector<std::wstring>> cache;
  static std::vector<size_t> match(const Part& part,const std::string& key,size_t at){
    std::vector<size_t> positions{at};
    for(auto& s:part.syllables){
      std::vector<size_t> next;
      for(auto p:positions){
        if(key.compare(p,s.size(),s)==0)next.push_back(p+s.size());
        if(p<key.size()&&key[p]==s[0])next.push_back(p+1);
      }
      std::sort(next.begin(),next.end());next.erase(std::unique(next.begin(),next.end()),next.end());positions=std::move(next);
    }return positions;
  }
public:
  std::pair<std::string,std::string> spelling(const std::wstring& text)const{
    for(auto& surname:surnames)if(text.compare(0,surname.text.size(),surname.text)==0&&text.size()>surname.text.size()&&text.size()<=surname.text.size()+2){
      std::string full,abbr;for(auto& s:surname.syllables){full+=s;abbr+=s[0];}
      bool valid=true;for(size_t i=surname.text.size();i<text.size();i++){
        auto p=std::find_if(given.begin(),given.end(),[&](const Part& g){return g.text==text.substr(i,1);});
        if(p==given.end()){valid=false;break;}full+=p->syllables[0];abbr+=p->syllables[0][0];
      }if(valid)return {full,abbr};
    }return {};
  }
  void load(const std::wstring& path){
    FILE* f=nullptr;_wfopen_s(&f,path.c_str(),L"rb");if(!f)return;char line[1024];
    while(fgets(line,sizeof(line),f)){
      std::string row=line;auto a=row.find('\t'),b=row.find('\t',a+1),c=row.find('\t',b+1);
      if(a==std::string::npos||b==std::string::npos||c==std::string::npos)continue;
      auto utf=row.substr(a+1,b-a-1);int n=MultiByteToWideChar(CP_UTF8,0,utf.data(),(int)utf.size(),nullptr,0);
      Part p;p.text.resize(n);MultiByteToWideChar(CP_UTF8,0,utf.data(),(int)utf.size(),p.text.data(),n);
      std::istringstream in(row.substr(b+1,c-b-1));std::string s;while(in>>s)p.syllables.push_back(s);
      p.rank=(float)atof(row.c_str()+c+1);if(!p.syllables.empty())(row[0]=='S'?surnames:given).push_back(std::move(p));
    }fclose(f);
  }
  std::vector<std::wstring> query(const std::string& key){
    if(key.size()<2||key.size()>24||key.find_first_not_of("abcdefghijklmnopqrstuvwxyz")!=std::string::npos)return {};
    auto old=cache.find(key);if(old!=cache.end())return old->second;
    std::unordered_map<std::wstring,float> found;
    std::unordered_map<size_t,std::vector<std::pair<const Part*,size_t>>> transitions;
    std::function<void(size_t,int,std::wstring,float)> visit=[&](size_t at,int depth,std::wstring text,float rank){
      if(at==key.size()){if(depth) {auto it=found.find(text);if(it==found.end()||rank<it->second)found[text]=rank;}return;}
      if(depth==2)return;
      auto it=transitions.find(at);
      if(it==transitions.end()){
        std::vector<std::pair<const Part*,size_t>> next;
        for(auto& p:given)for(auto end:match(p,key,at))next.push_back({&p,end});
        it=transitions.emplace(at,std::move(next)).first;
      }
      // Recursive inserts can rehash the table, so copy this short match list.
      auto next=it->second;
      for(auto& item:next){auto& p=*item.first;visit(item.second,depth+1,text+p.text,rank+p.rank*(depth?1.f:.15f));}
    };
    for(auto& p:surnames)for(auto end:match(p,key,0))if(end<key.size())visit(end,0,p.text,p.rank);
    std::vector<std::pair<std::wstring,float>> sorted(found.begin(),found.end());
    std::sort(sorted.begin(),sorted.end(),[](auto& a,auto& b){return a.second!=b.second?a.second<b.second:a.first<b.first;});
    std::vector<std::wstring> result;for(auto& p:sorted){if(result.size()==512)break;result.push_back(p.first);}
    if(cache.size()>=128)cache.clear();cache[key]=result;return result;
  }
};
