package monodroid.dialog;


public class BooleanElement
	extends monodroid.dialog.BoolElement
	implements
		mono.android.IGCUserPeer,
		android.widget.CompoundButton.OnCheckedChangeListener
{
	static final String __md_methods;
	static {
		__md_methods = 
			"n_onCheckedChanged:(Landroid/widget/CompoundButton;Z)V:GetOnCheckedChanged_Landroid_widget_CompoundButton_ZHandler:Android.Widget.CompoundButton/IOnCheckedChangeListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("MonoDroid.Dialog.BooleanElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", BooleanElement.class, __md_methods);
	}


	public BooleanElement ()
	{
		super ();
		if (getClass () == BooleanElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.BooleanElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	public BooleanElement (java.lang.String p0)
	{
		super ();
		if (getClass () == BooleanElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.BooleanElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.String, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", this, new java.lang.Object[] { p0 });
	}

	public BooleanElement (java.lang.String p0, int p1)
	{
		super ();
		if (getClass () == BooleanElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.BooleanElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.String, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e:System.Int32, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", this, new java.lang.Object[] { p0, p1 });
	}


	public void onCheckedChanged (android.widget.CompoundButton p0, boolean p1)
	{
		n_onCheckedChanged (p0, p1);
	}

	private native void n_onCheckedChanged (android.widget.CompoundButton p0, boolean p1);

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
