// SPDX-License-Identifier: GPL-3.0-or-later
// 数萌AI服务器 (winintel) / Alex YANG
#pragma once
// POSIX storage and UTF-8 adapters for the existing, shared offline engine.
#include <algorithm>
#include <chrono>
#include <codecvt>
#include <cstring>
#include <cstdio>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <locale>
#include <map>
#include <string>
#include <thread>
#include <vector>
#include <fcntl.h>
#include <sys/file.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <unistd.h>
#ifdef __APPLE__
#include <mach-o/dyld.h>
#endif
using DWORD=unsigned long;
using ULONGLONG=unsigned long long;
using BOOL=int;
constexpr int FALSE=0,CP_UTF8=65001,MB_ERR_INVALID_CHARS=8,MAX_PATH=4096;
constexpr int GENERIC_READ=0,FILE_SHARE_READ=0,OPEN_EXISTING=0,PAGE_READONLY=0,FILE_MAP_READ=0;
constexpr int MOVEFILE_REPLACE_EXISTING=1,MOVEFILE_WRITE_THROUGH=2,GetFileExInfoStandard=0,ERROR_FILE_NOT_FOUND=2;
constexpr DWORD WAIT_OBJECT_0=0,WAIT_ABANDONED=0x80,WAIT_TIMEOUT=0x102;
struct PosixHandle {int fd;};
using HANDLE=PosixHandle*;
#define INVALID_HANDLE_VALUE ((HANDLE)(intptr_t)-1)
struct LARGE_INTEGER {long long QuadPart;};
struct FILETIME {unsigned dwLowDateTime=0,dwHighDateTime=0;};
struct WIN32_FILE_ATTRIBUTE_DATA {FILETIME ftLastWriteTime;};
inline std::string posixUtf8(const std::wstring& s){
  return std::wstring_convert<std::codecvt_utf8<wchar_t>>().to_bytes(s);
}
inline std::wstring posixWide(const std::string& s){
  return std::wstring_convert<std::codecvt_utf8<wchar_t>>().from_bytes(s);
}
inline std::string posixPath(const wchar_t* s){
  auto p=posixUtf8(s);std::replace(p.begin(),p.end(),'\\','/');return p;
}
inline int MultiByteToWideChar(int,int,const char* src,int length,wchar_t* dst,int size){
  try{
    auto s=posixWide(length<0?std::string(src):std::string(src,length));if(length<0)s.push_back(0);
    if(!dst)return (int)s.size();if(size<(int)s.size())return 0;
    std::copy(s.begin(),s.end(),dst);return (int)s.size();
  }catch(...){return 0;}
}
inline int WideCharToMultiByte(int,int,const wchar_t* src,int length,char* dst,int size,void*,void*){
  try{
    auto s=posixUtf8(length<0?std::wstring(src):std::wstring(src,length));if(length<0)s.push_back(0);
    if(!dst)return (int)s.size();if(size<(int)s.size())return 0;
    std::copy(s.begin(),s.end(),dst);return (int)s.size();
  }catch(...){return 0;}
}
inline int _wfopen_s(FILE** out,const wchar_t* path,const wchar_t* mode){
  *out=fopen(posixPath(path).c_str(),posixUtf8(mode).c_str());return *out?0:errno;
}
inline unsigned long long _strtoui64(const char* s,char** end,int base){return strtoull(s,end,base);}
inline DWORD GetCurrentProcessId(){return getpid();}
inline ULONGLONG GetTickCount64(){
  return std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now().time_since_epoch()).count();
}
inline int GetLastError(){return errno;}
inline int GetFileAttributesExW(const wchar_t* path,int,WIN32_FILE_ATTRIBUTE_DATA* out){
  struct stat s;if(stat(posixPath(path).c_str(),&s))return 0;
#ifdef __APPLE__
  uint64_t t=(uint64_t)s.st_mtimespec.tv_sec*1000000000+s.st_mtimespec.tv_nsec;
#else
  uint64_t t=(uint64_t)s.st_mtim.tv_sec*1000000000+s.st_mtim.tv_nsec;
#endif
  out->ftLastWriteTime={(unsigned)t,(unsigned)(t>>32)};return 1;
}
inline int CompareFileTime(const FILETIME* a,const FILETIME* b){
  uint64_t x=((uint64_t)a->dwHighDateTime<<32)|a->dwLowDateTime,y=((uint64_t)b->dwHighDateTime<<32)|b->dwLowDateTime;
  return x==y?0:x<y?-1:1;
}
inline HANDLE CreateFileW(const wchar_t* path,int,int,void*,int,int,void*){
  int fd=open(posixPath(path).c_str(),O_RDONLY|O_CLOEXEC);return fd<0?INVALID_HANDLE_VALUE:new PosixHandle{fd};
}
inline int GetFileSizeEx(HANDLE h,LARGE_INTEGER* out){
  struct stat s;if(fstat(h->fd,&s))return 0;out->QuadPart=s.st_size;return 1;
}
inline HANDLE CreateFileMappingW(HANDLE h,void*,int,int,int,void*){
  int fd=dup(h->fd);return fd<0?nullptr:new PosixHandle{fd};
}
inline std::map<const void*,size_t> mappedViews;
inline void* MapViewOfFile(HANDLE h,int,int,int,int){
  struct stat s;if(fstat(h->fd,&s))return nullptr;
  void* p=mmap(nullptr,s.st_size,PROT_READ,MAP_PRIVATE,h->fd,0);if(p==MAP_FAILED)return nullptr;
  mappedViews[p]=s.st_size;return p;
}
inline void UnmapViewOfFile(const void* p){
  auto it=mappedViews.find(p);if(it!=mappedViews.end()){munmap(const_cast<void*>(p),it->second);mappedViews.erase(it);}
}
inline void CloseHandle(HANDLE h){if(h&&h!=INVALID_HANDLE_VALUE){close(h->fd);delete h;}}
inline std::string userDataRoot(){
  if(const char* p=getenv("WININTEL_DATA_HOME"))return p;
  if(const char* p=getenv("XDG_DATA_HOME"))return p;
  const char* h=getenv("HOME");return std::string(h?h:".")+
#ifdef __APPLE__
    "/Library/Application Support";
#else
    "/.local/share";
#endif
}
inline DWORD GetEnvironmentVariableW(const wchar_t* key,wchar_t* out,int size){
  std::string value;if(std::wstring(key)==L"LOCALAPPDATA")value=userDataRoot();
  else{auto p=getenv(posixUtf8(key).c_str());if(p)value=p;}
  auto w=posixWide(value);if(w.size()+1>(size_t)size)return 0;
  std::copy(w.begin(),w.end(),out);out[w.size()]=0;return w.size();
}
inline int CreateDirectoryW(const wchar_t* path,void*){
  std::error_code e;std::filesystem::create_directories(posixPath(path),e);return !e;
}
inline HANDLE CreateMutexW(void*,int,const wchar_t* name){
  uint64_t hash=1469598103934665603ULL;for(auto ch:std::wstring(name)){hash^=ch;hash*=1099511628211ULL;}
  auto folder=userDataRoot()+"/winintel-ime-native";std::error_code e;std::filesystem::create_directories(folder,e);
  int fd=open((folder+"/lock-"+std::to_string(hash)).c_str(),O_CREAT|O_RDWR|O_CLOEXEC,0600);
  return fd<0?nullptr:new PosixHandle{fd};
}
inline DWORD WaitForSingleObject(HANDLE h,int timeout){
  auto until=GetTickCount64()+timeout;
  do{if(flock(h->fd,LOCK_EX|LOCK_NB)==0)return WAIT_OBJECT_0;
    if(errno!=EWOULDBLOCK&&errno!=EAGAIN)return WAIT_TIMEOUT;
    std::this_thread::sleep_for(std::chrono::milliseconds(5));
  }while(GetTickCount64()<until);return WAIT_TIMEOUT;
}
inline void ReleaseMutex(HANDLE h){flock(h->fd,LOCK_UN);}
inline int MoveFileExW(const wchar_t* from,const wchar_t* to,int){
  auto src=posixPath(from),dst=posixPath(to);
  int fd=open(src.c_str(),O_RDONLY);if(fd<0)return 0;int ok=fsync(fd);close(fd);if(ok)return 0;
  if(rename(src.c_str(),dst.c_str()))return 0;
  fd=open(std::filesystem::path(dst).parent_path().c_str(),O_RDONLY);if(fd>=0){fsync(fd);close(fd);}return 1;
}
inline int DeleteFileW(const wchar_t* path){return unlink(posixPath(path).c_str())==0;}
inline std::wstring iniValue(const wchar_t* section,const wchar_t* key,const wchar_t* fallback,const wchar_t* path){
  std::ifstream f(posixPath(path));std::string row;bool active=false;
  while(std::getline(f,row)){
    if(!row.empty()&&row.back()=='\r')row.pop_back();
    if(row.size()>1&&row[0]=='['){active=row=="["+posixUtf8(section)+"]";continue;}
    auto at=row.find('=');if(active&&at!=std::string::npos&&row.substr(0,at)==posixUtf8(key))return posixWide(row.substr(at+1));
  }return fallback;
}
inline DWORD GetPrivateProfileStringW(const wchar_t* section,const wchar_t* key,const wchar_t* fallback,wchar_t* out,int size,const wchar_t* path){
  auto s=iniValue(section,key,fallback,path);s.resize(std::min<size_t>(s.size(),size-1));
  std::copy(s.begin(),s.end(),out);out[s.size()]=0;return s.size();
}
inline int GetPrivateProfileIntW(const wchar_t* section,const wchar_t* key,int fallback,const wchar_t* path){
  auto s=iniValue(section,key,L"",path);return s.empty()?fallback:wcstol(s.c_str(),nullptr,10);
}
inline DWORD GetModuleFileNameW(void*,wchar_t* out,int size){
  char path[MAX_PATH]={};
#ifdef __APPLE__
  uint32_t n=sizeof(path);if(_NSGetExecutablePath(path,&n))return 0;
#else
  auto n=readlink("/proc/self/exe",path,sizeof(path)-1);if(n<0)return 0;path[n]=0;
#endif
  auto w=posixWide(path);if(w.size()>=(size_t)size)return 0;
  std::copy(w.begin(),w.end(),out);out[w.size()]=0;return w.size();
}
