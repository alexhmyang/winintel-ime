// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
#include "Engine.h"
#include <chrono>
#include <cstdio>
int wmain(int argc,wchar_t** argv){
  if(argc<2)return 2;Engine engine;if(!engine.open(argv[1]))return 3;
  struct Test{const char* code;const wchar_t* expected;};
  Test tests[]={{"nihao",L"你好"},{"zg",L"中国"},{"shurufa",L"输入法"},{"nihai",L"你好"},{"nihoa",L"你好"},{"nhao",L"你好"},{"hello",L"hello"},
    {"yhm",L"杨慧明"},{"yanghuiming",L"杨慧明"},{"yanghm",L"杨慧明"},{"yhuiming",L"杨慧明"},
    {"zhanghaoyu",L"张浩宇"},{"chenjiaming",L"陈嘉明"},{"oyzh",L"欧阳子涵"},{"simayuhan",L"司马雨涵"},{"shanhaoming",L"单浩明"},
    {"zhonghuarenmingongheguo",L"中华人民共和国"},{"xinz hishengchanli",L"新质生产力"}};
  int failures=0;
  puts("| 输入 | 结果 | 耗时 ms |\n|---|---|---:|");
  for(auto& test:tests){std::string code=test.code;code.erase(std::remove(code.begin(),code.end(),' '),code.end());auto start=std::chrono::steady_clock::now();auto rows=engine.query(code);double ms=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();
    bool found=std::any_of(rows.begin(),rows.end(),[&](const Candidate& c){return c.text==test.expected;});printf("| %s | %s | %.3f |\n",test.code,found?"PASS":"FAIL",ms);if(!found)failures++;
  }
  auto names=engine.query("yhm");auto name=std::find_if(names.begin(),names.end(),[](const Candidate& c){return c.text==L"杨慧明";});
  bool visible=name!=names.end()&&name-names.begin()<9;printf("| yhm first page | %s | 0 |\n",visible?"PASS":"FAIL");if(!visible)failures++;
  engine.learn("yhm",L"杨慧明");
  engine.learn("shm",L"单浩明");
  {Engine reloaded;if(!reloaded.open(argv[1]))return 3;
    for(auto code:{"yhm","yanghuiming","shanhaoming"}){auto result=reloaded.query(code);bool learned=!result.empty()&&result[0].text==(std::string(code)=="shanhaoming"?L"单浩明":L"杨慧明");
      printf("| %s learned reload | %s | 0 |\n",code,learned?"PASS":"FAIL");if(!learned)failures++;}}
  auto start=std::chrono::steady_clock::now();for(int i=0;i<100;i++)engine.query("rengongzhineng");printf("100 queries: %.2f ms\n",std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count());
  return failures?1:0;
}
