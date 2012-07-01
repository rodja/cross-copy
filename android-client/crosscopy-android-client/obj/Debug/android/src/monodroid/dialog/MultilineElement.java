package monodroid.dialog;


public class MultilineElement
	extends monodroid.dialog.EntryElement
	implements
		mono.android.IGCUserPeer
{
	static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("MonoDroid.Dialog.MultilineElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", MultilineElement.class, __md_methods);
	}


	public MultilineElement ()
	{
		super ();
		if (getClass () == MultilineElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.MultilineElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	public MultilineElement (java.lang.String p0)
	{
		super ();
		if (getClass () == MultilineElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.MultilineElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.String, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", this, new java.lang.Object[] { p0 });
	}

	public MultilineElement (java.lang.String p0, int p1)
	{
		super ();
		if (getClass () == MultilineElement.class)
			mono.android.TypeManager.Activate ("MonoDroid.Dialog.MultilineElement, MonoDroid.Dialog, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "System.String, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e:System.Int32, mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", this, new java.lang.Object[] { p0, p1 });
	}

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
