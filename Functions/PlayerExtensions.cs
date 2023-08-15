using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using CreativePlayers.Functions.Data;

using Obj = UnityEngine.Object;

using RTFunctions.Functions;

namespace CreativePlayers.Functions
{
    public static class PlayerExtensions
    {
        public static GameObject textMeshPro;
        public static Material fontMaterial;
        public static Font inconsolataFont = Font.GetDefault();

        public static float Pitch
        {
            get
            {
                float pitch = AudioManager.inst.pitch;
                if (pitch < 0f)
                {
                    pitch = -pitch;
                }

                if (pitch == 0f)
                    pitch = 0.0001f;

                return pitch;
            }
        }

        public static void AddCustomObject(int index)
        {
            var currentModel = PlayerPlugin.CurrentModel(index);
            if (currentModel != null)
            {
                currentModel.CreateCustomObject();
            }
        }

        public static void DuplicateCustomObject(string id, int index)
        {
            var currentModel = PlayerPlugin.CurrentModel(index);
            if (currentModel != null)
            {
                currentModel.DuplicateObject(id);
            }
        }
        
        public static void RemoveCustomObject(string id, int index)
        {
            var currentModel = PlayerPlugin.CurrentModel(index);
            if (currentModel != null)
            {
                ((Dictionary<string, object>)currentModel.values["Custom Objects"]).Remove(id);
            }
        }

        public static RTPlayer GetRTPlayer(this InputDataManager.CustomPlayer _customPlayer)
        {
            return PlayerPlugin.players.Find(x => x.playerIndex == _customPlayer.index);
        }

        public static List<PlayerModelClass.PlayerModel> GetPlayerModels()
        {
            return PlayerPlugin.playerModels.Values.ToList();
        }

        public static string GetPlayerModelIndex(int index)
        {
            return PlayerPlugin.playerModelsIndex[index];
        }

        public static void SetPlayerModelIndex(int index, int _id)
        {
            string e = PlayerPlugin.playerModels.ElementAt(_id).Key;

            PlayerPlugin.playerModelsIndex[index] = e;
        }

        public static int GetPlayerModelInt(PlayerModelClass.PlayerModel _model)
        {
            return PlayerPlugin.playerModels.Values.ToList().IndexOf(_model);
        }

        public static T GetItem<T>(this T _list, int index)
        {
            var list = _list.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_list) as T[];

            return list[index];
        }

        public static Vector2 ToVector2(this Vector3 _v)
        {
            return new Vector2(_v.x, _v.y);
        }

        public static void GetResources()
        {
            var findFolder = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
                              where x.name == "folder"
                              select x).ToList();

            var findButton = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
                              where x.name == "Text Element"
                              select x).ToList();

            textMeshPro = findButton[0].transform.GetChild(1).gameObject;
            fontMaterial = findButton[0].transform.GetChild(1).GetComponent<TextMeshProUGUI>().fontMaterial;
        }

        public static GameObject CreateCanvas(string _name = "")
        {
            string n = _name;
            if (n == "")
            {
                n = "Canvas";
            }
            var inter = new GameObject(n);
            inter.transform.localScale = Vector3.one * RTHelpers.screenScale;
            var interfaceRT = inter.AddComponent<RectTransform>();
            interfaceRT.anchoredPosition = new Vector2(960f, 540f);
            interfaceRT.sizeDelta = new Vector2(1920f, 1080f);
            interfaceRT.pivot = new Vector2(0.5f, 0.5f);
            interfaceRT.anchorMin = Vector2.zero;
            interfaceRT.anchorMax = Vector2.zero;

            var canvas = inter.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = RTHelpers.screenScale;

            var canvasScaler = inter.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            Debug.LogFormat("{0}Canvas Scale Factor: {1}\nResoultion: {2}", PlayerPlugin.className, canvas.scaleFactor, new Vector2(Screen.width, Screen.height));

            inter.AddComponent<GraphicRaycaster>();

            return inter;
        }


        public static Dictionary<string, object> GenerateUIImage(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent);
            gameObject.layer = 5;

            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());
            dictionary.Add("CanvasRenderer", gameObject.AddComponent<CanvasRenderer>());
            dictionary.Add("Image", gameObject.AddComponent<Image>());

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIText(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent);
            gameObject.layer = 5;

            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());
            dictionary.Add("CanvasRenderer", gameObject.AddComponent<CanvasRenderer>());
            var text = gameObject.AddComponent<Text>();
            text.font = Font.GetDefault();
            text.fontSize = 20;
            dictionary.Add("Text", text);

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUITextMeshPro(string _name, Transform _parent, bool _noFont = false)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = Obj.Instantiate(textMeshPro);
            gameObject.name = _name;
            gameObject.transform.SetParent(_parent);

            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.GetComponent<RectTransform>());
            dictionary.Add("CanvasRenderer", gameObject.GetComponent<CanvasRenderer>());
            var text = gameObject.GetComponent<TextMeshProUGUI>();

            if (_noFont)
            {
                var refer = MaterialReferenceManager.instance;
                var dictionary2 = (Dictionary<int, TMP_FontAsset>)refer.GetType().GetField("m_FontAssetReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(refer);

                TMP_FontAsset tmpFont;
                if (dictionary2.ToList().Find(x => x.Value.name == "Arial").Value != null)
                {
                    tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Arial").Value;
                }
                else
                {
                    tmpFont = dictionary2.ToList().Find(x => x.Value.name == "Liberation Sans SDF").Value;
                }

                text.font = tmpFont;
                text.fontSize = 20;
            }

            dictionary.Add("Text", text);

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIInputField(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var image = GenerateUIImage(_name, _parent);
            var text = GenerateUIText("text", ((GameObject)image["GameObject"]).transform);
            var placeholder = GenerateUIText("placeholder", ((GameObject)image["GameObject"]).transform);

            SetRectTransform((RectTransform)text["RectTransform"], new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));
            SetRectTransform((RectTransform)placeholder["RectTransform"], new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));

            dictionary.Add("GameObject", image["GameObject"]);
            dictionary.Add("RectTransform", image["RectTransform"]);
            dictionary.Add("Image", image["Image"]);
            dictionary.Add("Text", text["Text"]);
            dictionary.Add("Placeholder", placeholder["Text"]);
            var inputField = ((GameObject)image["GameObject"]).AddComponent<InputField>();
            inputField.textComponent = (Text)text["Text"];
            inputField.placeholder = (Text)placeholder["Text"];
            dictionary.Add("InputField", inputField);

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIButton(string _name, Transform _parent)
        {
            var gameObject = GenerateUIImage(_name, _parent);
            gameObject.Add("Button", ((GameObject)gameObject["GameObject"]).AddComponent<Button>());

            return gameObject;
        }

        public static Dictionary<string, object> GenerateUIToggle(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent);
            dictionary.Add("GameObject", gameObject);
            dictionary.Add("RectTransform", gameObject.AddComponent<RectTransform>());

            var bg = GenerateUIImage("Background", gameObject.transform);
            dictionary.Add("Background", bg["GameObject"]);
            dictionary.Add("BackgroundRT", bg["RectTransform"]);
            dictionary.Add("BackgroundImage", bg["Image"]);

            var checkmark = GenerateUIImage("Checkmark", ((GameObject)bg["GameObject"]).transform);
            dictionary.Add("Checkmark", checkmark["GameObject"]);
            dictionary.Add("CheckmarkRT", checkmark["RectTransform"]);
            dictionary.Add("CheckmarkImage", checkmark["Image"]);

            var toggle = gameObject.AddComponent<Toggle>();
            toggle.image = (Image)bg["Image"];
            toggle.targetGraphic = (Image)bg["Image"];
            toggle.graphic = (Image)checkmark["Image"];
            dictionary.Add("Toggle", toggle);

            ((Image)checkmark["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

            GetImage((Image)checkmark["Image"], "BepInEx/plugins/Assets/editor_gui_checkmark.png");

            return dictionary;
        }

        public static Dictionary<string, object> GenerateUIDropdown(string _name, Transform _parent)
        {
            var dictionary = new Dictionary<string, object>();
            var dropdownBase = GenerateUIImage(_name, _parent);
            dictionary.Add("GameObject", dropdownBase["GameObject"]);
            dictionary.Add("RectTransform", dropdownBase["RectTransform"]);
            dictionary.Add("Image", dropdownBase["Image"]);
            var dropdownD = ((GameObject)dropdownBase["GameObject"]).AddComponent<Dropdown>();
            dictionary.Add("Dropdown", dropdownD);

            var label = GenerateUIText("Label", ((GameObject)dropdownBase["GameObject"]).transform);
            ((Text)label["Text"]).color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);
            ((Text)label["Text"]).alignment = TextAnchor.MiddleLeft;

            var arrow = GenerateUIImage("Arrow", ((GameObject)dropdownBase["GameObject"]).transform);
            var arrowImage = (Image)arrow["Image"];
            arrowImage.color = new Color(0.2157f, 0.2157f, 0.2196f, 1f);
            GetImage(arrowImage, "BepInEx/plugins/Assets/editor_gui_left.png");
            ((GameObject)arrow["GameObject"]).transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            SetRectTransform((RectTransform)label["RectTransform"], new Vector2(-15.3f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-46.6f, 0f));
            SetRectTransform((RectTransform)arrow["RectTransform"], new Vector2(-2f, -0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f), new Vector2(32f, 32f));

            var template = GenerateUIImage("Template", ((GameObject)dropdownBase["GameObject"]).transform);
            SetRectTransform((RectTransform)template["RectTransform"], new Vector2(0f, 2f), Vector2.right, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0f, 192f));
            var scrollRect = ((GameObject)template["GameObject"]).AddComponent<ScrollRect>();


            var viewport = GenerateUIImage("Viewport", ((GameObject)template["GameObject"]).transform);
            SetRectTransform((RectTransform)viewport["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, Vector2.up, Vector2.zero);
            var mask = ((GameObject)viewport["GameObject"]).AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var scrollbar = GenerateUIImage("Scrollbar", ((GameObject)template["GameObject"]).transform);
            SetRectTransform((RectTransform)scrollbar["RectTransform"], Vector2.zero, Vector2.one, Vector2.right, Vector2.one, new Vector2(20f, 0f));
            var ssbar = ((GameObject)scrollbar["GameObject"]).AddComponent<Scrollbar>();

            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(((GameObject)scrollbar["GameObject"]).transform);
            slidingArea.layer = 5;
            var slidingAreaRT = slidingArea.AddComponent<RectTransform>();
            SetRectTransform(slidingAreaRT, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f));

            var handle = GenerateUIImage("Handle", slidingArea.transform);
            SetRectTransform((RectTransform)handle["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
            ((Image)handle["Image"]).color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);

            var content = new GameObject("Content");
            content.transform.SetParent(((GameObject)viewport["GameObject"]).transform);
            content.layer = 5;
            var contentRT = content.AddComponent<RectTransform>();
            SetRectTransform(contentRT, Vector2.zero, Vector2.one, Vector2.up, new Vector2(0.5f, 1f), new Vector2(0f, 32f));

            scrollRect.content = contentRT;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = ssbar;
            scrollRect.viewport = (RectTransform)viewport["RectTransform"];
            ssbar.handleRect = (RectTransform)handle["RectTransform"];
            ssbar.direction = Scrollbar.Direction.BottomToTop;
            ssbar.numberOfSteps = 0;

            var item = new GameObject("Item");
            item.transform.SetParent(content.transform);
            item.layer = 5;
            var itemRT = item.AddComponent<RectTransform>();
            SetRectTransform(itemRT, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));
            var itemToggle = item.AddComponent<Toggle>();

            var itemBackground = GenerateUIImage("Item Background", item.transform);
            SetRectTransform((RectTransform)itemBackground["RectTransform"], Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            ((Image)itemBackground["Image"]).color = new Color(0.9608f, 0.9608f, 0.9608f, 1f);

            var itemCheckmark = GenerateUIImage("Item Checkmark", item.transform);
            SetRectTransform((RectTransform)itemCheckmark["RectTransform"], new Vector2(8f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 32f));
            var itemCheckImage = (Image)itemCheckmark["Image"];
            itemCheckImage.color = new Color(0.1216f, 0.1216f, 0.1216f, 1f);
            GetImage(itemCheckImage, "BepInEx/plugins/Assets/editor_gui_diamond.png");

            var itemLabel = GenerateUIText("Item Label", item.transform);
            SetRectTransform((RectTransform)itemLabel["RectTransform"], new Vector2(15f, 0.5f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-50f, -3f));
            var itemLabelText = (Text)itemLabel["Text"];
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.font = inconsolataFont;
            itemLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemLabelText.verticalOverflow = VerticalWrapMode.Truncate;
            itemLabelText.text = "Option A";
            itemLabelText.color = new Color(0.1961f, 0.1961f, 0.1961f, 1f);

            itemToggle.image = (Image)itemBackground["Image"];
            itemToggle.targetGraphic = (Image)itemBackground["Image"];
            itemToggle.graphic = itemCheckImage;

            dropdownD.captionText = (Text)label["Text"];
            dropdownD.itemText = itemLabelText;
            dropdownD.alphaFadeSpeed = 0.15f;
            dropdownD.template = (RectTransform)template["RectTransform"];
            ((GameObject)template["GameObject"]).SetActive(false);

            return dictionary;
        }
        public static void SetRectTransform(RectTransform _rt, Vector2 _anchoredPos, Vector2 _anchorMax, Vector2 _anchorMin, Vector2 _pivot, Vector2 _sizeDelta)
        {
            _rt.anchoredPosition = _anchoredPos;
            _rt.anchorMax = _anchorMax;
            _rt.anchorMin = _anchorMin;
            _rt.pivot = _pivot;
            _rt.sizeDelta = _sizeDelta;
        }

        public static void GetImage(Image _image, string _filePath)
        {
            if (RTFile.FileExists(_filePath))
            {
                SpriteManager.inst.StartCoroutine(SpriteManager.GetSprite(RTFile.ApplicationDirectory + _filePath, new SpriteManager.SpriteLimits(), delegate (Sprite cover)
                {
                    _image.sprite = cover;
                }, delegate (string errorFile)
                {
                    _image.sprite = ArcadeManager.inst.defaultImage;
                }));
            }
        }

        public static string ConvertHealthToEquals(int _num, int _max = 3)
        {
            string str = "[";
            for (int i = 0; i < _num; i++)
            {
                str += "=";
            }

            int e = -_num + _max;
            if (e > 0)
            {
                for (int i = 0; i < e; i++)
                {
                    str += " ";
                }
            }

            return str += "]";
        }
    }
}
