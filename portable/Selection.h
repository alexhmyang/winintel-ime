// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
#pragma once
#include <algorithm>
#include <cmath>
#include <string>
#include <vector>
struct Cell {double x,y,w,h;};
struct Selection {
  static constexpr int pageSize=25;
  int selected=0,pending=0;
  int start()const{return selected/pageSize*pageSize;}
  void reset(){selected=0;pending=0;}
  bool page(int direction,int count){
    pending=0;int next=start()+direction*pageSize;
    if(next<0||next>=count)return false;selected=next;return true;
  }
  // Return an absolute index to commit, or -1 while awaiting a second digit.
  int digit(int d,int count){
    int local=pending?pending*10+d:d;pending=0;
    if(local==1||local==2){pending=local;return -1;}
    int index=start()+local-1;return local>0&&local<=25&&index<count?index:-1;
  }
  int confirm(int count){
    int index=pending?start()+pending-1:selected;pending=0;
    return index>=0&&index<count?index:-1;
  }
  void move(int dx,int dy,const std::vector<Cell>& cells){
    pending=0;int local=selected-start();if(local<0||local>=(int)cells.size())return;
    auto a=cells[local];double cx=a.x+a.w/2,cy=a.y+a.h/2,best=1e30;int target=local;
    double nearestRow=1e30;
    if(dy)for(auto b:cells){double delta=(b.y+b.h/2-cy)*dy;if(delta>1&&delta<nearestRow)nearestRow=delta;}
    for(int i=0;i<(int)cells.size();i++){
      auto b=cells[i];double x=b.x+b.w/2,y=b.y+b.h/2;
      if(dx&&(std::abs(y-cy)>1||(x-cx)*dx<=0))continue;
      if(dy&&std::abs((y-cy)*dy-nearestRow)>1)continue;
      double score=dx?std::abs(x-cx):std::abs(x-cx);
      if(score<best){best=score;target=i;}
    }selected=start()+target;
  }
};
