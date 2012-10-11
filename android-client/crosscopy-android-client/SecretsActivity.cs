using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using CrossCopy.BL;
using System.Collections.Generic;

namespace CrossCopy.AndroidClient
{
        [Activity (Label = "cross copy", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustPan)]
        public class SecretsActivity : Activity
        {
                #region Private members
                //	DialogAdapter rootDA;
                //	EntryElement secretEntry; 
                //	Section secretsSection;
                SecretListAdapter _secretListAdap;
                ListView dataList;
                string listData;
#endregion

                #region Methods
                protected override void OnCreate (Bundle bundle)
                {
                        base.OnCreate (bundle);
                        SetContentView (Resource.Layout.MainScreen);

                        var sl = new List<SecretsList> {
                                new SecretsList { Secret="Secret1", Devices = "1 device", Image = Resource.Drawable.remove },
                                new SecretsList { Secret="Secret2", Devices = "2 devices",Image = Resource.Drawable.remove },
                                new SecretsList { Secret="Secret3", Devices = "3 devices",Image = Resource.Drawable.remove },
                                new SecretsList { Secret="Secret4", Devices = "2 devices",Image = Resource.Drawable.remove }
                        };
                        _secretListAdap = new SecretListAdapter (this, sl);
                        var listView = FindViewById<ListView> (Resource.Id.listViewSecrets);
                        listView.Adapter = _secretListAdap;
                        listView.ItemClick += listView_ItemClick;


                        /*var normalButton = FindViewById<Button>(Resource.Id.btnListen);
                        dataList = FindViewById<ListView>(Resource.Id.dataList);
                        var txtEditSecret = FindViewById<EditText>(Resource.Id.txtEditSecret);
                        normalButton.Click += (sender, args) => {
                        Toast.MakeText(this, "Normal button clicked", ToastLength.Short).Show();
                        if (!String.IsNullOrEmpty (txtEditSecret.Text))
                        {
                        Secret newSecret = new Secret (txtEditSecret.Text);
                        CrossCopyApp.HistoryData.Secrets.Add (newSecret);
                        DisplaySecretDetail (newSecret);
                        if(listData.Length>0)
                        {
                        listData = listData + ",";
                        listData = listData + txtEditSecret.Text.Trim();
                        }
                        else
                        {
                        listData = listData + txtEditSecret.Text.Trim();
                        }
                        string [] myArr = listData.Split (',');
                        if (myArr.Length > 0) {
                        IListAdapter myAdapter= new ArrayAdapter<string>(this,Resource.Layout.listitem,myArr);
                        dataList.Adapter = myAdapter;
                        }
                        }

                        };*/
                }

                void listView_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
                {
                        var item = _secretListAdap.GetItem (e.Position);
                        Toast.MakeText (this, " Clicked!", ToastLength.Short).Show ();
                }

                protected override void OnResume ()
                {
                        base.OnResume ();

                        CrossCopyApp.Srv.Abort ();

                        FindViewById<Button> (Resource.Id.buttonGo).Click += (sender, e) => {
                                var phrase = FindViewById<EditText> (Resource.Id.editTextSecret).Text;
                                if (String.IsNullOrEmpty (phrase))
                                        return;

                                var newSecret = new Secret (phrase);
                                CrossCopyApp.HistoryData.Secrets.Add (newSecret);

                                DisplaySecretDetail (phrase);
                        };

                        //foreach (Secret s in CrossCopyApp.HistoryData.Secrets) {
                        //secretsSection.Add((Element)new StringElement (s.Phrase));
                        //}
                        //if (secretsSection.Count == 0) {
                        //  ((RootElement) secretsSection.Parent).RemoveAt (0);
                        //            }
                }

                private void DisplaySecretDetail (string phrase)
                {
                        var sessionIntent = new Intent ();
                        sessionIntent.SetClass (this, typeof(SessionActivity));
                        sessionIntent.PutExtra ("Secret", phrase);
                        StartActivity (sessionIntent);
                }
#endregion
        }
}


