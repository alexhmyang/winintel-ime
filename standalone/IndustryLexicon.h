// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
#pragma once
// Included after Candidate has been declared by Engine.h.
class IndustryLexicon {
  struct Word {std::string code;std::wstring text;float score;};
  struct Pack {std::unordered_map<std::string,std::vector<Word>> chinese;std::vector<Word> english;};
  std::unordered_map<std::string,Pack> packs;
  std::string selected="general";std::wstring settings;bool overridden=false,protect=true;ULONGLONG checked=0;
  static std::wstring decode(const std::string& s){
    int n=MultiByteToWideChar(CP_UTF8,MB_ERR_INVALID_CHARS,s.data(),(int)s.size(),nullptr,0);
    if(n<=0)return L"";std::wstring result(n,0);MultiByteToWideChar(CP_UTF8,MB_ERR_INVALID_CHARS,s.data(),(int)s.size(),&result[0],n);return result;
  }
  static std::string lower(std::string s){for(char& c:s)c=(char)tolower((unsigned char)c);return s;}
public:
  void open(const std::wstring& root){
    FILE* f=nullptr;_wfopen_s(&f,(root+L"\\industry-words.tsv").c_str(),L"rb");
    if(f){char line[2048];unsigned count=0;while(fgets(line,sizeof(line),f)&&++count<=50000){
      std::string row=line;while(!row.empty()&&(row.back()=='\n'||row.back()=='\r'))row.pop_back();
      std::vector<std::string> fields;size_t start=0;
      for(;;){auto end=row.find('\t',start);fields.push_back(row.substr(start,end-start));if(end==std::string::npos)break;start=end+1;}
      if(fields.size()!=5||fields[0].size()>32||fields[2].empty()||fields[2].size()>128)continue;
      auto text=decode(fields[3]);if(text.empty()||text.size()>128)continue;
      Word word{lower(fields[2]),text,(float)atof(fields[4].c_str())};
      auto& pack=packs[fields[0]];if(fields[1]=="C")pack.chinese[word.code].push_back(word);else if(fields[1]=="E")pack.english.push_back(word);
    }fclose(f);}
    wchar_t app[MAX_PATH];if(GetEnvironmentVariableW(L"LOCALAPPDATA",app,MAX_PATH))settings=std::wstring(app)+L"\\winintel-ime-native\\industry.ini";
    refresh();
  }
  void refresh(){
    if(overridden||settings.empty()||GetTickCount64()-checked<250)return;checked=GetTickCount64();
    wchar_t value[64];GetPrivateProfileStringW(L"Industry",L"Profile",L"general",value,64,settings.c_str());
    std::wstring wide=value;std::string id(wide.begin(),wide.end());selected=packs.count(id)?id:"general";
    protect=GetPrivateProfileIntW(L"Industry",L"CodeAssist",1,settings.c_str())!=0;
  }
  void setProfile(const std::string& id){selected=packs.count(id)?id:"general";overridden=true;}
  std::string profile(){refresh();return selected;}
  bool codeHost(){
    refresh();if(selected!="programming"||!protect)return false;
    wchar_t path[MAX_PATH]={};GetModuleFileNameW(nullptr,path,MAX_PATH);std::wstring name=path;
    auto at=name.find_last_of(L"\\/");if(at!=std::wstring::npos)name=name.substr(at+1);
    for(wchar_t& c:name)c=towlower(c);
    static const std::unordered_set<std::wstring> editors={L"code.exe",L"code - insiders.exe",L"devenv.exe",L"idea64.exe",L"pycharm64.exe",L"webstorm64.exe",L"clion64.exe",L"rider64.exe",L"goland64.exe",L"rustrover64.exe",L"sublime_text.exe",L"notepad++.exe",L"windowsterminal.exe"};
    return editors.count(name)!=0;
  }
  void appendChinese(const std::string& input,std::vector<Candidate>& result){
    refresh();auto p=packs.find(selected);if(p==packs.end())return;
    auto found=p->second.chinese.find(lower(input));if(found==p->second.chinese.end())return;
    for(const auto& w:found->second){auto c=std::find_if(result.begin(),result.end(),[&](const Candidate& r){return r.text==w.text;});
      if(c==result.end())result.push_back({w.text,w.score});else c->score=std::max(c->score,w.score);}
  }
  bool englishToken(const std::string& typed){
    refresh();auto p=packs.find(selected);if(p==packs.end())return false;auto key=lower(typed);
    return std::any_of(p->second.english.begin(),p->second.english.end(),[&](const Word& w){return w.code==key;});
  }
  void appendEnglish(const std::string& typed,std::vector<Candidate>& result){
    refresh();auto p=packs.find(selected);if(p==packs.end())return;auto key=lower(typed);if(key.empty())return;
    for(const auto& w:p->second.english)if(w.code.compare(0,key.size(),key)==0){
      float score=(w.code==key?100000.f:0)+1000+w.score-(float)(w.code.size()-key.size());
      auto c=std::find_if(result.begin(),result.end(),[&](const Candidate& r){return r.text==w.text;});
      if(c==result.end())result.push_back({w.text,score});else c->score=std::max(c->score,score);
    }
  }
};
