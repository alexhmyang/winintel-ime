// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
#pragma once
#include <string>
#include <vector>
#include <algorithm>
// Editing stays inside the uncommitted buffer; host document shortcuts remain intact.
class Composition {
  struct Snapshot { std::string text; size_t caret; };
  std::string& text;
  std::vector<Snapshot> past,future;
  void Save(){if(past.size()==32)past.erase(past.begin());past.push_back({text,caret});future.clear();}
public:
  static constexpr size_t limit=256;
  size_t caret=0;
  explicit Composition(std::string& value):text(value){}
  void Clear(){text.clear();caret=0;past.clear();future.clear();}
  void Move(size_t position){caret=std::min(position,text.size());}
  bool Insert(char value){if(text.size()>=limit)return false;Save();text.insert(caret++,1,value);return true;}
  bool Back(){if(!caret)return false;Save();text.erase(--caret,1);return true;}
  bool Delete(){if(caret>=text.size())return false;Save();text.erase(caret,1);return true;}
  bool Undo(bool redo=false){
    auto& from=redo?future:past;auto& to=redo?past:future;
    if(from.empty())return false;
    to.push_back({text,caret});auto state=std::move(from.back());from.pop_back();
    text=std::move(state.text);caret=state.caret;return true;
  }
};
