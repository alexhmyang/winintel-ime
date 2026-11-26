// SPDX-License-Identifier: GPL-3.0-or-later
#include "../Native/Composition.h"
#include "../../standalone/Engine.h"
#include <chrono>
#include <iostream>
#include <stdexcept>
void Check(bool ok,const char* label){std::cout<<"| "<<label<<" | "<<(ok?"PASS":"FAIL")<<" |\n";if(!ok)throw std::runtime_error(label);}
int wmain(int argc,wchar_t** argv){
  if(argc<2)return 2;
  std::cout<<"| Check | Result |\n|---|---|\n";
  std::string text;Composition edit(text);
  for(char c:std::string("nihxo"))edit.Insert(c);
  edit.Move(3);edit.Delete();edit.Insert('a');
  Check(text=="nihao"&&edit.caret==4,"repair middle without losing suffix");
  edit.Undo();Check(text=="niho"&&edit.caret==3,"undo insertion restores caret");
  edit.Undo();Check(text=="nihxo","undo deletion restores suffix");
  edit.Undo(true);edit.Undo(true);Check(text=="nihao","redo repairs original typo");
  edit.Move(0);Check(!edit.Back()&&text=="nihao","backspace at start keeps text");
  edit.Move(text.size());Check(!edit.Delete(),"delete at end keeps text");
  edit.Move(0);edit.Insert('a');edit.Undo();edit.Insert('b');Check(!edit.Undo(true),"new edit clears redo");
  edit.Clear();Check(text.empty()&&!edit.Undo(),"commit clears history");
  for(size_t i=0;i<300;i++)edit.Insert('a');
  Check(text.size()==256&&edit.caret==256,"256 limit without overwriting tail");
  edit.Move(100);edit.Delete();edit.Insert('b');
  Check(text.size()==256&&text[100]=='b'&&text.substr(101)==std::string(155,'a'),"edit middle of maximum length");
  bool history=true;for(int i=0;i<32;i++)history=edit.Undo()&&history;
  Check(history,"32 previous edits remain available");
  Check(!edit.Undo(),"history stops at 32 edits");
  Engine engine;Check(engine.open(argv[1]),"offline dictionary loads");
  std::string longCode;
  for(int i=0;i<40;i++)longCode+="nihao";
  auto start=std::chrono::steady_clock::now();auto rows=engine.query(longCode);
  double ms=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();
  std::wstring expected;for(int i=0;i<40;i++)expected+=L"你好";
  Check(!rows.empty()&&rows[0].text==expected,"200-letter whole sentence decoding");
  std::cout<<"| 200-letter query milliseconds | "<<ms<<" |\n";
  engine.learn(longCode,expected);
  Engine reopened;Check(reopened.open(argv[1])&&reopened.hasPreference(longCode),"long sentence preference survives dictionary reopen");
}
