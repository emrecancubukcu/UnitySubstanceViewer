using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Substance.Game;
using Substance.Platform;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Random = UnityEngine.Random;

public class SubstanceViewer : MonoBehaviour
{
    public SubstanceGraph mySubstance;
    private NativeTypes.Input[] inputs;

    private int inputCount;
    Vector2 scrollPosition;

    public GUISkin guiSkin;
    public int col1 = 30;
    public int col2 = 30;
    public int col3 = 30;

    public int enumCol2 = 30;
    public int colorCol3 = 30;

    public Texture2D colorPicker;
    public RenderTexture ResultTexture;
    public int size = 2048;

    public Texture2D emptyTexture;
    private string[] Tabs = { "Parameters", "Presets", "Settings" };
    public int selectedTab = 0;
    private string[] sizeStrings = { "256", "512", "1024", "2048", "4096" };
    private int[] sizes = { 256, 512, 1024, 2048, 4096 };
    public int selectedSize = 1;

    public string[] presetList;
    public FileInfo[] fileInfo;
    public int width = 300;
    public Color copiedColor;

    public int randomSeed = 0;

    void GUIInputs()
    {
        GUILayout.BeginVertical("Box");
        scrollPosition = GUILayout.BeginScrollView(
                  scrollPosition, GUILayout.Width(width + 20));

        GUI.changed = false;

        string lastGroupName = "";

        int no = 0;
        foreach (NativeTypes.Input input in inputs)
        {
            no++;
          //  Debug.Log(input.label+" "+input.group+" "+ input.unityInputType.ToString());
            if (no < 2)
            {
                continue;
            }

            if (lastGroupName != input.group)
            {
                GUILayout.Space(8);
                GUILayout.Box(input.group);
                GUILayout.Space(8);
            }

            lastGroupName = input.group;
            if (input.unityInputType == NativeTypes.UnityInputType.Boolean)
            {

                GUILayout.BeginHorizontal();
                GUILayout.Label(input.label, GUILayout.Width(col1));

                bool val = mySubstance.GetInputBool(input.name); ;
                val = GUILayout.Toggle(val, "");
                mySubstance.SetInputBool(input.name, val);
                GUILayout.EndHorizontal();

            }
            else
            if (input.unityInputType == NativeTypes.UnityInputType.Enum)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(input.label, GUILayout.Width(col1));
                int listCount = 0;
                NativeTypes.MComboBoxItem[] items;

                IntPtr iptr = NativeFunctions.cppGetMComboBoxItems(mySubstance.nativeHandle, input.name, out listCount);
                items = new NativeTypes.MComboBoxItem[listCount];

                for (int i = 0; i < listCount; i++)
                {
                    items[i] = new NativeTypes.MComboBoxItem();
                    items[i] = (NativeTypes.MComboBoxItem)Marshal.PtrToStructure(iptr, typeof(NativeTypes.MComboBoxItem));
                    iptr = new IntPtr(iptr.ToInt64() + Marshal.SizeOf(items[i]));
                }


                int number = 0;
                string[] labels = new string[listCount];
                int minVal = 1000;
                foreach (NativeTypes.MComboBoxItem item in items)
                {
                    labels[number] = item.label;
                    if (minVal > item.value)
                        minVal = item.value;
                    number++;
                }
                int _val = mySubstance.GetInputInt(input.name); ;

                int val = _val - minVal;
                GUILayout.BeginVertical("Box", GUILayout.Width(enumCol2));
                val = GUILayout.SelectionGrid(val, labels, 1);
                GUILayout.EndVertical();


                mySubstance.SetInputInt(input.name, val + minVal);
                GUILayout.EndHorizontal();
            }
            else
            if (input.unityInputType == NativeTypes.UnityInputType.Float)
            {
                GUILayout.BeginHorizontal();
                float val = mySubstance.GetInputFloat(input.name);
                GUILayout.Label(input.label, GUILayout.Width(col1));
                val = GUILayout.HorizontalSlider(val, input.minimum.x, input.maximum.x, GUILayout.Width(col2));
                GUILayout.Label(" " + (Mathf.Round(val * 100f) / 100).ToString(""), GUILayout.Width(col3));
                mySubstance.SetInputFloat(input.name, val);
                GUILayout.EndHorizontal();
            }
            else
            if (input.unityInputType == NativeTypes.UnityInputType.Color)
            {
                GUILayout.BeginHorizontal();
                Color val = mySubstance.GetInputColor(input.name, 0);
                GUILayout.Label(input.label, GUILayout.Width(col1));
                Rect creatureRect = GUILayoutUtility.GetLastRect();
                GUILayout.BeginVertical();

                if (GUILayout.Button(colorPicker, GUILayout.Width(100), GUILayout.Height(100)))
                {
                    Vector2 mousePosition = Event.current.mousePosition;
                    float currentPickerPosX = mousePosition.x - col1 - 0.0f;
                    float currentPickerPosY = (mousePosition.y - creatureRect.y + 0.0f) * (1.0f);

                    int x = Convert.ToInt32(currentPickerPosX);
                    int y = Convert.ToInt32(currentPickerPosY);
                    val = colorPicker.GetPixel(Mathf.RoundToInt((float)colorPicker.width / 100 * x), Mathf.RoundToInt((float)colorPicker.height / 100 * (100 - y)));
                    mySubstance.SetInputColor(input.name, val);
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("copy", GUILayout.Width(45)))
                {
                    copiedColor = val;

                }
                if (GUILayout.Button("Paste", GUILayout.Width(45)))
                {
                    val = copiedColor;
                    mySubstance.SetInputColor(input.name, val);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUI.contentColor = new Color(val.r, val.g, val.b, 1f);
                GUILayout.Label(emptyTexture, GUILayout.Width(colorCol3));
                GUI.contentColor = Color.white;

                GUILayout.EndHorizontal();
            }
            else
                GUILayout.Label(" - " + input.label + input.unityInputType.ToString(""));

            GUILayout.Space(4);


        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

    }

    void GUIPresets()
    {

        GUILayout.BeginVertical("Box");
        scrollPosition = GUILayout.BeginScrollView(
          scrollPosition, GUILayout.Width(width + 20));

        foreach (FileInfo file in fileInfo)
            if (file.Extension == ".sbsprs")
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(file.Name), GUILayout.Width(width - 30)))
                {
                    LoadPreset(file.FullName);
                    UpdateSubstance();

                }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        if (GUILayout.Button("R A N D O M"))
        {
            RandomizeValues();
        }

    }

    void GUISettings()
    {
        int oldSize = size;
        GUILayout.BeginHorizontal("Box");
        selectedSize = GUILayout.SelectionGrid(selectedSize, sizeStrings, 5);
        size = sizes[selectedSize];
        if (size != oldSize)
        {
            mySubstance.SetTexturesResolution(new Vector2Int(size, size));
            UpdateSubstance();
        }
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Export"))
        {
            BakeTexture();
        }
        GUILayout.EndHorizontal();

    }

    void OnGUI()
    {

        if (mySubstance == null)
            return;
        GUI.skin.box.fixedWidth = 0;
        GUILayout.BeginArea(new Rect(10, 10, width + 20, Screen.height - 15));

        GUILayout.Box(mySubstance.material.name);
        GUILayout.Space(8);


        GUILayout.BeginHorizontal("Box");
        selectedTab = GUILayout.SelectionGrid(selectedTab, Tabs, 5);

        GUILayout.EndHorizontal();

        if (selectedTab == 0)
        {
            GUIInputs();
        }
        else
        if (selectedTab == 1)
        {
            GUIPresets();
        }
        else
            GUISettings();

        GUILayout.EndArea();

        if (GUI.changed)
            UpdateSubstance();

    }


    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    public static Color StringToVector4(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');
        Color result = new Color();
        // store as a Vector3
        if (sArray.Length == 3)
        {
            result = new Color(
         float.Parse(sArray[0]),
         float.Parse(sArray[1]),
         float.Parse(sArray[2]),
         1f);

        }
        else
        {
           result = new Color(
           float.Parse(sArray[0]),
           float.Parse(sArray[1]),
           float.Parse(sArray[2]),
           float.Parse(sArray[3]));
        }


        return result;
    }

    void RandomizeValues()
    {
        Random.seed =  Random.Range(0, 100000);

        int inputNo = 0;
        foreach (NativeTypes.Input input in inputs)
        {

            if (input.name.Contains("_norandom"))
                continue;

            if (input.unityInputType == NativeTypes.UnityInputType.Boolean)
            {
                int val = Random.Range(0, 2);
                mySubstance.SetInputInt(input.name, val);
            }
            if (input.unityInputType == NativeTypes.UnityInputType.Enum)
            {
                int listCount = 0;
                NativeTypes.MComboBoxItem[] items;

                IntPtr iptr = NativeFunctions.cppGetMComboBoxItems(mySubstance.nativeHandle, input.name, out listCount);

                int val = Convert.ToInt32(Random.Range(0, listCount));
                mySubstance.SetInputInt(input.name, val);
            }
            if (input.unityInputType == NativeTypes.UnityInputType.Float)
            {
                float val = Random.Range(input.minimum.x, input.maximum.x);
                mySubstance.SetInputFloat(input.name, val);

            }
            if (input.unityInputType == NativeTypes.UnityInputType.Color)
            {

                Color val = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                mySubstance.SetInputColor(input.name, val);
            }

            inputNo++;
        }

        mySubstance.SetInputInt("$randomseed", Random.Range(0, 1000));

    }

    void LoadPreset(string filename)
    {
        StreamReader reader = new StreamReader(filename);

        string txt = reader.ReadToEnd();
        reader.Close();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(txt);
        XmlNodeList presetInputs = xmlDoc.GetElementsByTagName("presetinput");


        foreach (XmlNode presetInput in presetInputs)
        {
            foreach (NativeTypes.Input input in inputs)
            {
                bool found = false;
                if (input.name == presetInput.Attributes["identifier"].Value)
                {

                    if (input.unityInputType == NativeTypes.UnityInputType.Boolean)
                    {
                        int val = Convert.ToInt32(presetInput.Attributes["value"].Value);
                        mySubstance.SetInputInt(input.name, val);

                        found = true;
                    }
                    if (input.unityInputType == NativeTypes.UnityInputType.Enum)
                    {
                        int val = Convert.ToInt32(presetInput.Attributes["value"].Value);
                        mySubstance.SetInputInt(input.name, val);
                        found = true;
                    }
                    if (input.unityInputType == NativeTypes.UnityInputType.Float)
                    {
                        float val = float.Parse(presetInput.Attributes["value"].Value);
                        mySubstance.SetInputFloat(input.name, val);
                        found = true;
                    }
                    if (input.unityInputType == NativeTypes.UnityInputType.Color)
                    {
                        Color val = StringToVector4(presetInput.Attributes["value"].Value);
                        mySubstance.SetInputColor(input.name, val);
                        found = true;
                    }

                }

            }
        }
    }
    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US"); 
        Material mat = null;
        mat = this.GetComponent<Renderer>().sharedMaterial;
        inputCount = NativeFunctions.cppGetNumInputs(mySubstance.nativeHandle);
        inputs = NativeTypes.GetNativeInputs(mySubstance, inputCount);
        mySubstance.SetTexturesResolution(new Vector2Int(size, size));
      
        UpdateSubstance();

        var info = new DirectoryInfo(Application.dataPath + "/presets");
        fileInfo = info.GetFiles();

    }

    IEnumerator DoCheck()
    {
        while (true)
        {
            UpdateSubstance();
            yield return new WaitForSeconds(.25f);
        }
    }

    public void UpdateSubstance()
    {

        mySubstance.QueueForRender();
        Substance.Game.Substance.RenderSubstancesSync();
      //  Substance.Game.Substance.RenderSubstancesAsync();

    }
    void BakeTexture()
    {
        Texture2D ResultTexture = new Texture2D(size, size);
        ResultTexture.name = "Baked Texture";
        mySubstance.Bake(ResultTexture, Application.persistentDataPath);

    }

    private void Update()
    {

        if (Input.GetKeyDown("r"))
        {
            mySubstance.SetInputInt("$randomseed", 150);
            UpdateSubstance();
        }

    }

}
