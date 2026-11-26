import com.winintel.ime.Dictionary;
import com.winintel.ime.Normalize;
import java.io.File;
public class DictionaryCheck {
 public static void main(String[] args)throws Exception{
  Dictionary d=new Dictionary(new File(args[0]));
  String[][] tests={{"nihao","你好"},{"shuru","输入"},{"zhongguo","中国"},{"nhao","你好"},{"nh","你好"}};
  System.out.println("| 拼音 | 目标候选 | 结果 | 耗时 ms |");
  System.out.println("|---|---|---|---|");
  for(String[] t:tests){long start=System.nanoTime();java.util.List<String> result=d.query(t[0]);double ms=(System.nanoTime()-start)/1e6;boolean ok=result.contains(t[1]);System.out.printf("| %s | %s | %s | %.2f |%n",t[0],t[1],ok?"通过":"失败",ms);if(!ok)throw new AssertionError(t[0]+result);}
  String output=Normalize.apply("V10032G s x m 三 n v link 五十六 g b p s");
  if(!output.equals("V100 32G SXM3 NVLink 56Gbps"))throw new AssertionError(output);
  if(!Normalize.apply("一起测试，万一失败，千万不要删除。").equals("一起测试，万一失败，千万不要删除。"))throw new AssertionError("Numeric idioms");
 }
}
