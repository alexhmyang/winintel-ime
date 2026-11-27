// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import android.content.Context;
import com.k2fsa.sherpa.onnx.*;
/** Called only from a worker. Same engine used by VoiceService and device verification. */
public final class SpeechEngine implements AutoCloseable {
    private final OfflineRecognizer recognizer;
    public SpeechEngine(Context context)throws Exception{
        OfflineSenseVoiceModelConfig sense=new OfflineSenseVoiceModelConfig();
        sense.setModel(Assets.copy(context,"sensevoice/model.int8.onnx").getPath());
        sense.setLanguage("auto");sense.setUseInverseTextNormalization(true);
        OfflineModelConfig models=new OfflineModelConfig();models.setSenseVoice(sense);
        models.setTokens(Assets.copy(context,"sensevoice/tokens.txt").getPath());models.setNumThreads(2);models.setProvider("cpu");
        OfflineRecognizerConfig config=new OfflineRecognizerConfig();config.setModelConfig(models);
        recognizer=new OfflineRecognizer(null,config);
    }
    public String decode(float[] audio){
        OfflineStream stream=recognizer.createStream();
        try{stream.acceptWaveform(audio,16000);recognizer.decode(stream);return Normalize.apply(recognizer.getResult(stream).getText());}
        finally{stream.release();}
    }
    @Override public void close(){recognizer.release();}
}
