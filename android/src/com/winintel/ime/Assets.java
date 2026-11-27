// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import android.content.Context;
import java.io.*;
public final class Assets {
    static File copy(Context context,String asset) throws IOException {
        return copy(context,asset,asset);
    }
    static File copy(Context context,String localName,String asset) throws IOException {
        File target=new File(context.getFilesDir(),localName);
        if(target.exists() && target.length()>0)return target;
        target.getParentFile().mkdirs();
        File temp=new File(target.getPath()+".tmp");
        try(InputStream in=context.getAssets().open(asset);OutputStream out=new FileOutputStream(temp)){
            byte[] buf=new byte[65536];int n;while((n=in.read(buf))!=-1)out.write(buf,0,n);
        }
        if(!temp.renameTo(target))throw new IOException("Cannot prepare "+asset);
        return target;
    }
}
