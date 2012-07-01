package monodroid.dialog;


public class RootElement
	extends monodroid.dialog.Element
	implements
		mono.android.IGCUserPeer,
		android.content.DialogInterface.OnClickListener
{
	static final String __md_methods;
	static {
		__md_methods = 
			"n_onClick:(Landroid/content/DialogInterface;I)V:GetOnClick_Landroid_content_DialogInterface_IHandler:Android.Content.IDialogInterfaceOnClickListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("MonoDroid.Dialog.RootElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", RootElement.class, __md_methods);
	}


	public RootElement ()
	{
		super ();
		if (getClass () == RootElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.RootElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	public RootElement (java.lang.String p0)
	{
		super ();
		if (getClass () == RootElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.RootElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.String, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", this, new java.lang.Object[] { p0 });
	}

	public RootElement (java.lang.String p0, int p1)
	{
		super ();
		if (getClass () == RootElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.RootElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.String, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e:System.Int32, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", this, new java.lang.Object[] { p0, p1 });
	}


	public void onClick (android.content.DialogInterface p0, int p1)
	{
		n_onClick (p0, p1);
	}

	private native void n_onClick (android.content.DialogInterface p0, int p1);

	java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
