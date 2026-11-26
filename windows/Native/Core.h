// SPDX-License-Identifier: GPL-3.0-or-later
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
  int dpi=96,hover=-1;
  unsigned selected=0,page=0,pages=0;
  static LRESULT CALLBACK Procedure(HWND,UINT,WPARAM,LPARAM);
public:
  std::function<void(int)> Page;
  std::function<void(unsigned)> Choose;
  ~CandidateView();
  void Show(ITfContext*,TfClientId,const std::string&,const std::vector<Candidate>&,unsigned);
  void Hide();
};
HRESULT Insert(ITfContext*,TfClientId,const std::wstring&);
HRESULT CreateService(REFIID,void**);
