// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
#pragma once
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <msctf.h>
#include <string>
#include <vector>
#include <functional>
#include "../../standalone/Engine.h"
#include "Paging.h"
extern HMODULE module;
extern long objects;
extern const CLSID ServiceId;
extern const GUID ProfileId;
std::wstring Root();
class CandidateView {
  HWND window=nullptr;
  std::wstring content;
  std::vector<std::wstring> visible;
  std::vector<RECT> cells;
  RECT previous={},next={};
  RECT header={};
  std::vector<int> advances;
  unsigned caret=0;
  int scroll=0;
  int dpi=96,hover=-1;
  int fontSize=20;
  HWND tooltip=nullptr;
  unsigned selected=0,page=0,pages=0;
  bool english=false;
  int navigationX=-1;
  static LRESULT CALLBACK Procedure(HWND,UINT,WPARAM,LPARAM);
public:
  unsigned pendingNumber=0;
  std::function<void(int)> Page;
  std::function<void(unsigned)> Choose;
  std::function<void(unsigned)> MoveCaret;
  ~CandidateView();
  void Show(ITfContext*,TfClientId,const std::string&,const std::vector<Candidate>&,unsigned,size_t caret=std::string::npos,bool english=false);
  void Hide();
  void ResetNavigation(){navigationX=-1;}
  unsigned MoveSelection(unsigned current,UINT key);
#ifdef WININTEL_TEST_STORE
  bool Cell(unsigned offset,RECT* rect)const{if(!rect||offset>=cells.size())return false;*rect=cells[offset];return true;}
#endif
};
HRESULT Insert(ITfContext*,TfClientId,const std::wstring&);
HRESULT CreateService(REFIID,void**);
