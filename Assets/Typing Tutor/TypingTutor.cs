using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Random = UnityEngine.Random;
using System;

public class TypingTutor : MonoBehaviour {

	public KMNeedyModule TTModule;
	public KMAudio Audio;
	public KMModSettings modSettings;
	public Transform TTTransform;
	public KMSelectable TTSelectable;
	public TTSettings Settings;
	public Transform Screens, Buttons;
	public KMSelectable Zero, One, Back, Enter;
	public TextAsset WordsFile;
	public Color Green, Red, White;
	private List<string> Words;
	public TextMesh InputText, UpperText;
	private int Successes;
	private string WordsFP, SettingsFP; //FP -> File Paths
	private bool isActive;
	private bool isSelected = false;
	private bool FilesFound;
	public static int ModuleID = 1;
	public int thisModuleID;
	private KeyCode[] LetterKeys =
	{
		KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G,
		KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M,
		KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S,
		KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z
	};
	// Use this for initialization
	void Start () {
		thisModuleID = ModuleID++;
		try
		{
			Words = JsonConvert.DeserializeObject<List<string>>(WordsFile.ToString());
			Settings = JsonConvert.DeserializeObject<TTSettings>(modSettings.Settings);

			FilesFound = true;
			DebugMsg("Module initialization successful.");
		} catch (Exception e)
		{
			if (e is DirectoryNotFoundException || e is JsonReaderException || e is FileNotFoundException)
			{
				FilesFound = false;
				UpperText.text = "Error";
				DebugMsg("Module initialization failed. Exception information: " + e.ToString());
				return;
			} throw;
		}
		if (Settings.NoKeyboard)
		{
			/* Uncomment if the below doesn't work
			Screens.Translate(0, 0, 0.016f);
			Buttons.Translate(0, 0.018f, 0);
			*/
			
			Screens.localPosition += new Vector3(0, 0, 0.016f);
			Buttons.localPosition += new Vector3(0, 0.018f, 0);

			Zero.OnInteract += delegate
			{
				if (isActive && InputText.text.Length < 8)
					InputText.text += "0";
				Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, TTTransform);
				return false;
			};
			One.OnInteract += delegate
			{
				if (isActive && InputText.text.Length < 8)
					InputText.text += "1";
				Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, TTTransform);
				return false;
			};
			Back.OnInteract += delegate
			{
				if (isActive && InputText.text.Length > 0)
					InputText.text = InputText.text.Substring(0, InputText.text.Length - 1);
				Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, TTTransform);
				return false;
			};
			Enter.OnInteract += delegate
			{
				if (isActive) Submit();
				Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, TTTransform);
				return false;
			};
		}
		TTModule.OnNeedyActivation += delegate
		{
			if (FilesFound) OnNeedyActivation();
		};
		TTModule.OnPass += delegate { isActive = false; return false; };
		TTModule.OnTimerExpired += OnTimerExpired;
		TTSelectable.OnFocus += delegate { isSelected = true; };
		TTSelectable.OnDefocus += delegate { isSelected = false; };
		isActive = false;
	}
	// Update is called once per frame
	void Update()
	{
		if (isActive)
		{
			if (!Settings.NoKeyboard && isSelected)
			{
				//Key checks
				foreach (KeyCode k in LetterKeys)
					if (Input.GetKeyDown(k) && InputText.text.Length < 8)
						InputText.text += k.ToString();
				if (Input.GetKeyDown(KeyCode.Backspace) && InputText.text.Length > 0)
					InputText.text = InputText.text.Substring(0, InputText.text.Length - 1);
				if (Input.GetKeyDown(KeyCode.Return)) Submit();
			}
			//Color checks
			if (InputText.text.EqualsIgnoreCase(UpperText.text)) InputText.color = Green;
			else if (!PartiallyEquals(InputText.text, UpperText.text)) InputText.color = Red;
			else InputText.color = White;
		}

	}
	void DebugMsg(string msg)
	{
		Debug.LogFormat("[Needy Typing Tutor #{0}] {1}", thisModuleID, msg);
	}
	//Postcondition: returns true if two strings are equal up to the length of the smaller string
	private bool PartiallyEquals(string s1, string s2)
	{
		char[] c1 = s1.ToUpper().ToCharArray(); char[] c2 = s2.ToUpper().ToCharArray();
		int length = Math.Min(c1.Length, c2.Length);
		for(int i = 0; i < length; i++)
			if (c1[i] != c2[i]) return false;
		return true;
	}
	private void OnNeedyActivation()
	{
		TTModule.SetNeedyTimeRemaining(30);
		SetNewWord();
		Successes = 0;
		isActive = true;
		DebugMsg("Module activated.");
	}
	private void OnTimerExpired()
	{
		InputText.text = ""; UpperText.text = "";
		if(FilesFound) TTModule.OnStrike();
		TTModule.OnPass();
		DebugMsg("Module time expired.");
	}
	private void Submit()
	{
		if (InputText.text.EqualsIgnoreCase(UpperText.text))
		{
			InputText.text = "";
			Successes++;
			if(Successes >= 3)
			{
				UpperText.text = "";
				TTModule.OnPass();
			}
			else
			{
				SetNewWord();
				Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyActivated, TTTransform);
				DebugMsg("Module disarmed.");
			}
		}
	}
	private void SetNewWord()
	{
		if (Settings.NoKeyboard)
		{
			string BitString = "";
			for (int i = 0; i < 8; i++)
				BitString += Random.Range(0, 2);
			UpperText.text = BitString;
		}
		else UpperText.text = Words[Random.Range(0, Words.Count)];
	}
	public class TTSettings
	{
		[JsonProperty("NoKeyboard")]
		public bool NoKeyboard;
	}
}
