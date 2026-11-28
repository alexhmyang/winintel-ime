// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
#pragma once
#ifdef __cplusplus
extern "C" {
#endif
void* winintel_open(const char* dictionary);
void winintel_close(void* engine);
int winintel_query(void* engine,const char* code,int english);
const char* winintel_candidate(void* engine,int index);
void winintel_learn(void* engine,const char* code,int index);
void winintel_industry(void* engine,const char* profile);
int winintel_selected(void* engine);
int winintel_pending(void* engine);
int winintel_digit(void* engine,int digit);
int winintel_confirm(void* engine);
void winintel_page(void* engine,int direction);
void winintel_cell(void* engine,int index,double x,double y,double w,double h);
void winintel_move(void* engine,int dx,int dy);
#ifdef __cplusplus
}
#endif
