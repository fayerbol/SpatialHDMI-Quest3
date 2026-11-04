package com.spatialhdmi.bridge;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import java.io.File;
import java.io.InputStream;
import java.io.FileOutputStream;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/** SAF: no broad storage permission, no conversion of content:// into a fake filesystem path. */
public final class MediaPickerActivity extends Activity {
    private String unityTarget;
    private final ExecutorService worker = Executors.newSingleThreadExecutor();
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        unityTarget = getIntent().getStringExtra("unityTarget");
        if (state != null) return;
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("video/*");
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try { startActivityForResult(intent, 41); }
        catch (Exception e) { send("ERROR: No compatible Android document picker"); }
    }
    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != 41) return;
        if (result != RESULT_OK || data == null || data.getData() == null) { send(""); return; }
        final Uri uri = data.getData();
        worker.execute(() -> {
            File part = new File(getFilesDir(), "selected-video.part");
            File destination = new File(getFilesDir(), "selected-video.mp4");
            try {
                try (InputStream input = getContentResolver().openInputStream(uri);
                     FileOutputStream output = new FileOutputStream(part)) {
                    if (input == null) throw new java.io.IOException("Cannot read selected video");
                    byte[] bytes = new byte[256 * 1024]; int n;
                    while ((n = input.read(bytes)) != -1) output.write(bytes, 0, n);
                }
                if (destination.exists() && !destination.delete()) throw new java.io.IOException("Cannot replace previous import");
                if (!part.renameTo(destination)) throw new java.io.IOException("Cannot finish import");
                send(destination.getAbsolutePath());
            } catch (Exception e) { part.delete(); send("ERROR: Video import: " + e.getMessage()); }
        });
    }
    private void send(final String result) {
        runOnUiThread(() -> {
            try {
                // Reflection keeps this small Android library independent of Unity's generated Java module.
                Class.forName("com.unity3d.player.UnityPlayer")
                    .getMethod("UnitySendMessage", String.class, String.class, String.class)
                    .invoke(null, unityTarget, "OnFileSelected", result);
            } catch (Exception ignored) { android.util.Log.e("SpatialHDMI", "Could not deliver file selection"); }
            finish(); worker.shutdown();
        });
    }
}
