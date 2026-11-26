#include "Engine.h"
#include <chrono>
#include <cstdio>
int wmain(int argc,wchar_t** argv){
  if(argc<2)return 2;Engine engine;if(!engine.open(argv[1]))return 3;
  struct Test{const char* code;const wchar_t* expected;};
  Test tests[]={{"nihao",L"你好"},{"zg",L"中国"},{"shurufa",L"输入法"},{"nihai",L"你好"},{"nihoa",L"你好"},{"nhao",L"你好"},{"hello",L"hello"}};
  int failures=0;
  puts("| 输入 | 结果 | 耗时 ms |\n|---|---|---:|");
  for(auto& test:tests){auto start=std::chrono::steady_clock::now();auto rows=engine.query(test.code);double ms=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();
    bool found=std::any_of(rows.begin(),rows.end(),[&](const Candidate& c){return c.text==test.expected;});printf("| %s | %s | %.3f |\n",test.code,found?"PASS":"FAIL",ms);if(!found)failures++;
  }
  auto start=std::chrono::steady_clock::now();for(int i=0;i<100;i++)engine.query("rengongzhineng");printf("100 queries: %.2f ms\n",std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count());
  return failures?1:0;
}
