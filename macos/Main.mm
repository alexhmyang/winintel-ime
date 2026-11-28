// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
#import <Cocoa/Cocoa.h>
#import <InputMethodKit/InputMethodKit.h>
#import <QuartzCore/QuartzCore.h>
#include "../portable/Bridge.h"
#include "../portable/Selection.h"
@interface CandidateDocument:NSView
@end
@implementation CandidateDocument
-(BOOL)isFlipped{return YES;}
@end
@interface WinintelController:IMKInputController {
  void* engine;
  NSString* code;
  BOOL english,shiftDown,shiftUsed;
  int count;
  NSPanel* panel;
  id activeClient;
}
@end
@implementation WinintelController
-(id)initWithServer:(IMKServer*)server delegate:(id)delegate client:(id)client {
  self=[super initWithServer:server delegate:delegate client:client];
  if(self){
    NSString* path=[[NSBundle mainBundle] pathForResource:@"dictionary" ofType:@"bin"];
    engine=winintel_open(path.fileSystemRepresentation);code=@"";
    panel=[[NSPanel alloc] initWithContentRect:NSMakeRect(0,0,640,200)
        styleMask:NSWindowStyleMaskNonactivatingPanel|NSWindowStyleMaskBorderless
        backing:NSBackingStoreBuffered defer:NO];
    panel.level=NSPopUpMenuWindowLevel;panel.hidesOnDeactivate=YES;
    panel.backgroundColor=NSColor.windowBackgroundColor;
  }return self;
}
-(NSUInteger)recognizedEvents:(id)sender{return NSEventMaskKeyDown|NSEventMaskFlagsChanged;}
-(void)clear:(id)sender {
  code=@"";
  [sender setMarkedText:@"" selectionRange:NSMakeRange(0,0) replacementRange:NSMakeRange(NSNotFound,NSNotFound)];
  [panel orderOut:nil];
}
-(void)commit:(int)index client:(id)sender {
  const char* utf=winintel_candidate(engine,index);if(!utf)return;
  NSString* text=[NSString stringWithUTF8String:utf];
  if(!english)winintel_learn(engine,code.UTF8String,index);
  [sender insertText:text replacementRange:NSMakeRange(NSNotFound,NSNotFound)];
  code=@"";[panel orderOut:nil];
}
-(void)clicked:(NSButton*)button{[self commit:(int)button.tag client:activeClient];}
-(void)pageClicked:(NSButton*)button {
  winintel_page(engine,(int)button.tag);[self refresh:NO client:activeClient];
}
-(void)refresh:(BOOL)query client:(id)sender {
  if(!code.length){[self clear:sender];return;}
  activeClient=sender;
  if(query)count=winintel_query(engine,code.UTF8String,english);
  [sender setMarkedText:code selectionRange:NSMakeRange(code.length,0) replacementRange:NSMakeRange(NSNotFound,NSNotFound)];
  int selected=winintel_selected(engine),start=selected/25*25,pending=winintel_pending(engine);
  CandidateDocument* document=[[CandidateDocument alloc] initWithFrame:NSMakeRect(0,0,640,200)];
  NSTextField* header=[NSTextField labelWithString:[NSString stringWithFormat:@"%@ · 第 %d / %d 页%@",
      code,start/25+1,(count+24)/25,pending?[NSString stringWithFormat:@" · 编号 %d…",pending]:@""]];
  header.frame=NSMakeRect(8,4,624,24);[document addSubview:header];
  double x=8,y=34,rowHeight=40;
  NSFont* font=[NSFont systemFontOfSize:18];
  for(int i=start;i<std::min(start+25,count);i++){
    NSString* text=[NSString stringWithUTF8String:winintel_candidate(engine,i)];
    NSString* title=[NSString stringWithFormat:@"%d %@",i-start+1,text];
    NSDictionary* attr=@{NSFontAttributeName:font};
    double width=std::min(624.,std::max(64.,std::ceil([title sizeWithAttributes:attr].width)+24));
    if(x+width>632){x=8;y+=rowHeight+4;rowHeight=40;}
    NSRect bound=[title boundingRectWithSize:NSMakeSize(width-16,1000)
        options:NSStringDrawingUsesLineFragmentOrigin attributes:attr];
    double height=std::max(40.,std::ceil(bound.size.height)+16);
    NSButton* button=[[NSButton alloc] initWithFrame:NSMakeRect(x,y,width,height)];
    button.title=title;button.font=font;button.bordered=NO;button.cell.wraps=YES;
    button.tag=i;button.target=self;button.action=@selector(clicked:);
    if(i==selected){
      button.wantsLayer=YES;button.layer.backgroundColor=[NSColor colorWithRed:0.09 green:0.54 blue:0.32 alpha:1].CGColor;
      button.attributedTitle=[[NSAttributedString alloc] initWithString:title attributes:@{NSFontAttributeName:font,NSForegroundColorAttributeName:NSColor.whiteColor}];
    }
    winintel_cell(engine,i-start,x,y,width,height);
    [document addSubview:button];x+=width+4;rowHeight=std::max(rowHeight,height);
  }
  y+=rowHeight+8;
  for(int direction=-1;direction<=1;direction+=2){
    NSButton* button=[NSButton buttonWithTitle:direction<0?@"上一页":@"下一页" target:self action:@selector(pageClicked:)];
    button.frame=NSMakeRect(direction<0?8:116,y,100,32);button.tag=direction;[document addSubview:button];
  }
  document.frame=NSMakeRect(0,0,640,y+40);
  NSScrollView* scroll=[[NSScrollView alloc] initWithFrame:NSMakeRect(0,0,640,std::min(y+40,360.))];
  scroll.hasVerticalScroller=YES;scroll.documentView=document;panel.contentView=scroll;
  NSRect caret=NSZeroRect;[sender attributesForCharacterIndex:0 lineHeightRectangle:&caret];
  NSScreen* screen=NSScreen.mainScreen;
  for(NSScreen* s in NSScreen.screens)if(NSIntersectsRect(s.frame,caret)){screen=s;break;}
  NSRect visible=screen.visibleFrame;double h=scroll.frame.size.height;
  double px=std::max(visible.origin.x,std::min(caret.origin.x,NSMaxX(visible)-640));
  double py=caret.origin.y-h;if(py<visible.origin.y)py=NSMaxY(caret);
  py=std::max(visible.origin.y,std::min(py,NSMaxY(visible)-h));
  [panel setFrame:NSMakeRect(px,py,640,h) display:YES];[panel orderFrontRegardless];
}
-(BOOL)handleEvent:(NSEvent*)event client:(id)sender {
  if(!engine)return NO;
  if(event.type==NSEventTypeFlagsChanged){
    BOOL down=(event.modifierFlags&NSEventModifierFlagShift)!=0;
    if(down&&!shiftDown){shiftDown=YES;shiftUsed=NO;}
    else if(!down&&shiftDown){
      shiftDown=NO;
      if(!shiftUsed){
        if(code.length)[sender insertText:code replacementRange:NSMakeRange(NSNotFound,NSNotFound)];
        english=!english;[self clear:sender];
      }
    }return NO;
  }
  shiftUsed=YES;
  if(event.modifierFlags&(NSEventModifierFlagCommand|NSEventModifierFlagControl|NSEventModifierFlagOption))return NO;
  unsigned short key=event.keyCode;
  if(code.length){
    if(key==53){[self clear:sender];return YES;}
    if(key==51){code=[code substringToIndex:code.length-1];[self refresh:YES client:sender];return YES;}
    if(key==49||key==36||key==48){[self commit:winintel_confirm(engine) client:sender];return YES;}
    if(key==116||key==121){winintel_page(engine,key==116?-1:1);[self refresh:NO client:sender];return YES;}
    if(key>=123&&key<=126){
      winintel_move(engine,key==123?-1:key==124?1:0,key==125?1:key==126?-1:0);
      [self refresh:NO client:sender];return YES;
    }
    NSString* chars=event.characters;
    if(chars.length==1&&[chars characterAtIndex:0]>='0'&&[chars characterAtIndex:0]<='9'){
      int index=winintel_digit(engine,[chars characterAtIndex:0]-'0');
      if(index>=0)[self commit:index client:sender];else [self refresh:NO client:sender];return YES;
    }
  }
  NSString* chars=event.characters;
  if(chars.length==1&&((chars.UTF8String[0]>='a'&&chars.UTF8String[0]<='z')||(chars.UTF8String[0]>='A'&&chars.UTF8String[0]<='Z'))&&code.length<256){
    code=[code stringByAppendingString:english?chars:chars.lowercaseString];[self refresh:YES client:sender];return YES;
  }
  if(code.length)[self commit:winintel_confirm(engine) client:sender];
  return NO;
}
-(void)deactivateServer:(id)sender{[self clear:sender];shiftDown=NO;[super deactivateServer:sender];}
-(void)dealloc{winintel_close(engine);}
@end
static IMKServer* retainedServer;
int main(){
  @autoreleasepool{
    [NSApplication sharedApplication];
    retainedServer=[[IMKServer alloc] initWithName:@"WinintelConnection" bundleIdentifier:@"com.winintel.ime.macos"];
    if(!retainedServer)return 1;[NSApp run];
  }return 0;
}
