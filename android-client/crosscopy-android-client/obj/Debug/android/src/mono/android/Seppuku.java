package mono.android;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.util.Log;

public class Seppuku extends BroadcastReceiver {
	@Override
	public void onReceive (Context context, Intent intent)
	{
		java.lang.Runtime.getRuntime ().exit (-1);
	}
}
