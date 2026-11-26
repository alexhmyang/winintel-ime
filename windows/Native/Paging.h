// SPDX-License-Identifier: GPL-3.0-or-later
#pragma once
#include <algorithm>
struct CandidatePaging {
  static constexpr unsigned size=9;
  static unsigned start(unsigned selected){return selected/size*size;}
  static unsigned pages(unsigned count){return (count+size-1)/size;}
  static unsigned move(unsigned selected,unsigned count,int direction){
    if(!count)return 0;
    unsigned page=start(selected)/size;
    if(direction<0)return page?((page-1)*size):selected;
    return page+1<pages(count)?((page+1)*size):selected;
  }
  static unsigned digit(unsigned selected,unsigned digit){return start(selected)+digit;}
};
