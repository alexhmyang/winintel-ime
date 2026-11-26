// SPDX-License-Identifier: GPL-3.0-or-later
#pragma once
#include <memory>
#include <mutex>
#include <thread>
#include <sddl.h>
struct RankingState {
  std::mutex mutex;std::string request,resultCode;int result=-1;bool running=false;
};
inline std::wstring RankPipe(){
  HANDLE token;std::wstring name=L"\\\\.\\pipe\\winintel-rank-";
  if(!OpenProcessToken(GetCurrentProcess(),TOKEN_QUERY,&token))return L"";
  DWORD size=0;GetTokenInformation(token,TokenUser,nullptr,0,&size);std::vector<BYTE> data(size);
  if(GetTokenInformation(token,TokenUser,data.data(),size,&size)){LPWSTR sid=nullptr;if(ConvertSidToStringSidW(((TOKEN_USER*)data.data())->User.Sid,&sid)){name+=sid;LocalFree(sid);}}
  CloseHandle(token);return name;
}
inline void RequestRanking(std::shared_ptr<RankingState> state,const std::string& code,const std::vector<Candidate>& rows){
  std::string request=code;
  // The optional model ranks only the first page; later pages stay local.
  for(size_t i=0;i<std::min<size_t>(9,rows.size());i++){auto& row=rows[i];int length=WideCharToMultiByte(CP_UTF8,0,row.text.c_str(),(int)row.text.size(),nullptr,0,nullptr,nullptr);std::string text(length,0);WideCharToMultiByte(CP_UTF8,0,row.text.c_str(),(int)row.text.size(),&text[0],length,nullptr,nullptr);request+="\t"+text;}
  request+="\n";
  {std::lock_guard<std::mutex> lock(state->mutex);state->request=request;if(state->running)return;state->running=true;}
  // TSF modules are retained for process lifetime, including bounded API workers.
  HMODULE retained;GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_PIN,(LPCWSTR)&RequestRanking,&retained);
  std::thread([state](){
    while(true){
      std::string request;{std::lock_guard<std::mutex> lock(state->mutex);request.swap(state->request);if(request.empty()){state->running=false;return;}}
      std::string code=request.substr(0,request.find('\t'));int result=-1;
      HANDLE pipe=CreateFileW(RankPipe().c_str(),GENERIC_READ|GENERIC_WRITE,0,nullptr,OPEN_EXISTING,0,nullptr);
      if(pipe!=INVALID_HANDLE_VALUE){DWORD transferred=0;char reply[32]={};if(WriteFile(pipe,request.data(),(DWORD)request.size(),&transferred,nullptr)&&ReadFile(pipe,reply,31,&transferred,nullptr)){reply[transferred]=0;result=atoi(reply);}CloseHandle(pipe);}
      {std::lock_guard<std::mutex> lock(state->mutex);state->resultCode=code;state->result=result;}
    }
  }).detach();
}
