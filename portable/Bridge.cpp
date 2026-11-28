// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
#include "Bridge.h"
#include "Selection.h"
#include "../standalone/EnglishLexicon.h"
#include <memory>
struct Session {Engine chinese;EnglishLexicon english;std::vector<Candidate> result;std::vector<std::string> utf;Selection selection;std::vector<Cell> cells;};
extern "C" {
void* winintel_open(const char* dictionary){
  try{
    auto s=std::make_unique<Session>();auto path=posixWide(dictionary);
    if(!s->chinese.open(path))return nullptr;
    s->english.open(path.substr(0,path.find_last_of(L"/\\")));return s.release();
  }catch(...){return nullptr;}
}
void winintel_close(void* e){delete static_cast<Session*>(e);}
int winintel_query(void* e,const char* code,int english){
  if(!e||!code)return 0;auto s=static_cast<Session*>(e);
  try{s->selection.reset();s->cells.clear();s->result=english?s->english.query(code):s->chinese.query(code);s->utf.clear();
    for(auto& c:s->result)s->utf.push_back(posixUtf8(c.text));return s->utf.size();
  }catch(...){s->result.clear();s->utf.clear();return 0;}
}
const char* winintel_candidate(void* e,int index){
  if(!e)return nullptr;auto s=static_cast<Session*>(e);
  return index>=0&&(size_t)index<s->utf.size()?s->utf[index].c_str():nullptr;
}
void winintel_learn(void* e,const char* code,int index){
  if(!e||!code)return;auto s=static_cast<Session*>(e);
  if(index>=0&&(size_t)index<s->result.size())try{s->chinese.learn(code,s->result[index].text);}catch(...){}
}
void winintel_industry(void* e,const char* profile){
  if(!e||!profile)return;auto s=static_cast<Session*>(e);s->chinese.setIndustry(profile);s->english.setIndustry(profile);
}
int winintel_selected(void* e){return e?static_cast<Session*>(e)->selection.selected:0;}
int winintel_pending(void* e){return e?static_cast<Session*>(e)->selection.pending:0;}
int winintel_digit(void* e,int digit){if(!e||digit<0||digit>9)return -1;auto s=static_cast<Session*>(e);return s->selection.digit(digit,s->utf.size());}
int winintel_confirm(void* e){if(!e)return -1;auto s=static_cast<Session*>(e);return s->selection.confirm(s->utf.size());}
void winintel_page(void* e,int direction){if(e){auto s=static_cast<Session*>(e);s->selection.page(direction,s->utf.size());}}
void winintel_cell(void* e,int index,double x,double y,double w,double h){
  if(!e||index<0||index>=25)return;auto s=static_cast<Session*>(e);
  if(index>=(int)s->cells.size())s->cells.resize(index+1);s->cells[index]={x,y,w,h};
}
void winintel_move(void* e,int dx,int dy){if(e){auto s=static_cast<Session*>(e);s->selection.move(dx,dy,s->cells);}}
}
