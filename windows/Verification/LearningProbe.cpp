// SPDX-License-Identifier: GPL-3.0-or-later
// All preferences are written to the supplied isolated LOCALAPPDATA fixture.
#include "../../standalone/Engine.h"
#include "../Native/Paging.h"
#include <chrono>
int failures=0;
void check(bool ok,const char* label){printf("%s: %s\n",label,ok?"PASS":"FAIL");if(!ok)failures++;}
int wmain(int argc,wchar_t** argv){
  if(argc<3)return 2;SetEnvironmentVariableW(L"LOCALAPPDATA",argv[2]);
  Engine engine;if(!engine.open(argv[1]))return 3;
  if(argc>3&&std::wstring(argv[3])==L"paging"){
    auto rows=engine.query("shi");if(rows.size()<19)return 4;
    FILE* f=nullptr;_wfopen_s(&f,(std::wstring(argv[2])+L"\\paging-expected.tsv").c_str(),L"wb");if(!f)return 5;
    for(unsigned index:{0u,9u,18u,(unsigned)rows.size()-1}){
      auto text=rows[index].text;int length=WideCharToMultiByte(CP_UTF8,0,text.data(),(int)text.size(),nullptr,0,nullptr,nullptr);std::string utf(length,0);
      WideCharToMultiByte(CP_UTF8,0,text.data(),(int)text.size(),&utf[0],length,nullptr,nullptr);
      fprintf(f,"%u\t%s\n",index,utf.c_str());
    }
    fclose(f);printf("shi candidates: %zu; pages: %u\n",rows.size(),CandidatePaging::pages((unsigned)rows.size()));return 0;
  }
  if(argc>3){
    auto key=std::string("parallel")+char('0'+_wtoi(argv[3]));
    for(int i=0;i<30;i++)engine.learn(key,L"并发学习");
    return 0;
  }
  check(engine.query("legacyfixture")[0].text==L"旧词","legacy choices.tsv imported");
  auto rows=engine.query("shi");if(rows.size()<3)return 4;
  check(rows.size()>9,"later candidates retained for paging");
  check(CandidatePaging::move(0,20,1)==9&&CandidatePaging::move(9,20,1)==18,"next page boundaries");
  check(CandidatePaging::move(18,20,1)==18&&CandidatePaging::move(0,20,-1)==0,"first and last page clamp");
  check(CandidatePaging::move(18,20,-1)==9&&CandidatePaging::digit(9,2)==11,"previous page and current page digit");
  check(CandidatePaging::pages(20)==3&&CandidatePaging::pages(9)==1,"partial last page count");
  auto original=rows[0].text,chosen=rows[2].text;
  Engine other;check(other.open(argv[1]),"second instance initialized before selection");
  engine.learn("shi",chosen);
  check(engine.query("shi")[0].text==chosen,"single selection moves candidate to first");
  check(other.query("shi")[0].text==chosen,"existing other instance reloads preference");
  {Engine restarted;restarted.open(argv[1]);check(restarted.query("shi")[0].text==chosen,"restart retains preference");}
  engine.learn("shi",original);
  check(engine.query("shi")[0].text==original,"latest selection changes first candidate");
  check(!engine.hasPreference("sh"),"different input has no inherited preference");
  engine.learn("SHI",chosen);
  check(engine.query("SHI")[0].text==chosen,"case normalization matches");
  engine.learn("brandfixture",L"数萌AI服务器");
  check(engine.query("brandfixture")[0].text==L"数萌AI服务器","learned phrase restored outside dictionary");
  engine.learn("invalid\tkey",L"错误");
  check(!engine.hasPreference("invalid\tkey"),"invalid key cannot corrupt preference file");
  // Two independent processes update distinct keys concurrently.
  wchar_t executable[MAX_PATH];GetModuleFileNameW(nullptr,executable,MAX_PATH);
  PROCESS_INFORMATION workers[2]={};
  for(int i=0;i<2;i++){
    std::wstring command=L"\""+std::wstring(executable)+L"\" \""+argv[1]+L"\" \""+argv[2]+L"\" "+std::to_wstring(i);
    STARTUPINFOW startup={sizeof(startup)};
    check(CreateProcessW(nullptr,&command[0],nullptr,nullptr,FALSE,CREATE_NO_WINDOW,nullptr,nullptr,&startup,&workers[i])!=FALSE,"start concurrent writer");
  }
  bool completed=true;
  for(auto& worker:workers)if(worker.hProcess){DWORD exitCode=1;completed=WaitForSingleObject(worker.hProcess,30000)==WAIT_OBJECT_0&&completed;GetExitCodeProcess(worker.hProcess,&exitCode);completed=completed&&exitCode==0;CloseHandle(worker.hProcess);CloseHandle(worker.hThread);}
  check(completed,"concurrent writers complete");
  engine.query("parallel0");
  check(engine.hasPreference("parallel0")&&engine.hasPreference("parallel1"),"both writers preserve records");
  auto begin=std::chrono::steady_clock::now();
  for(int i=0;i<100;i++)engine.query("rengongzhineng");
  printf("100 queries ms: %.2f\n",std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count());
  return failures?1:0;
}
