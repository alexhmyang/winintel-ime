// 数萌AI服务器 (winintel) / Alex YANG - portable Android engine verification.
package com.winintel.ime;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
public final class AccuracyBenchmark {
    public static void main(String[] args)throws Exception{
        Dictionary dictionary=new Dictionary(new File(args[0]));
        try(BufferedReader r=new BufferedReader(new InputStreamReader(new FileInputStream(args[1]),StandardCharsets.UTF_8));
            PrintWriter out=new PrintWriter(new OutputStreamWriter(new FileOutputStream(args[2]),StandardCharsets.UTF_8))){
            out.println("id\tgroup\tinput\texpected\ttop1\trank\tcandidateCount\tms\ttop9");
            String row;while((row=r.readLine())!=null){
                String[] f=row.split("\t");if(f.length!=4)throw new IOException("Malformed corpus");
                long start=System.nanoTime();List<String> words=dictionary.query(f[2]);double ms=(System.nanoTime()-start)/1e6;
                out.println(row+"\t"+(words.isEmpty()?"":words.get(0))+"\t"+(words.indexOf(f[3])+1)+"\t"+words.size()+"\t"+ms+"\t"+String.join(" / ",words.subList(0,Math.min(9,words.size()))));
            }
        }
    }
}
