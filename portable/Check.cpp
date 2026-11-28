// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
#include "Bridge.h"
#include "Selection.h"
#include <cassert>
#include <iostream>
#include <string>
int main(int argc,char** argv){
  Selection s;assert(s.digit(1,70)==-1);assert(s.digit(5,70)==14);
  assert(s.digit(2,70)==-1);assert(s.digit(5,70)==24);
  assert(s.digit(1,70)==-1);assert(s.confirm(70)==0);
  assert(s.page(1,70)&&s.start()==25);assert(s.digit(2,70)==-1);assert(s.digit(5,70)==49);
  assert(s.page(1,70)&&s.start()==50);assert(!s.page(1,70));
  assert(s.digit(2,70)==-1);assert(s.digit(5,70)==-1);
  std::vector<Cell> cells={{0,0,70,40},{70,0,130,40},{0,40,100,40},{100,40,100,40}};
  s.reset();s.move(0,1,cells);assert(s.selected==2);s.move(1,0,cells);assert(s.selected==3);
  s.move(0,-1,cells);assert(s.selected==1);s.move(1,0,cells);assert(s.selected==1);
  s.pending=1;s.move(-1,0,cells);assert(!s.pending&&s.selected==0);
  if(argc>1){
    void* engine=winintel_open(argv[1]);assert(engine);
    int n=winintel_query(engine,"nihao",0);assert(n>=25);
    std::string first=winintel_candidate(engine,0);assert(first=="你好");
    assert(winintel_candidate(engine,n-1));assert(!winintel_candidate(engine,n));
    int chosen=1;std::string word=winintel_candidate(engine,chosen);
    winintel_learn(engine,"nihao",chosen);winintel_close(engine);
    engine=winintel_open(argv[1]);assert(engine);winintel_query(engine,"nihao",0);
    assert(word==winintel_candidate(engine,0));
    winintel_industry(engine,"programming");assert(winintel_query(engine,"pyth",1)>0);
    winintel_close(engine);
    assert(!winintel_open("/nonexistent/winintel.bin"));
  }
  std::cout<<"PASS: paging, geometry, two-digit selection";
  if(argc>1)std::cout<<", real dictionary, English, persistent learning";
  std::cout<<std::endl;
}
