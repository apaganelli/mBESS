using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

namespace mBESS
{
    /// <summary>
    /// This class is aimed for editing a new participant.
    /// </summary>
    class ParticipantViewModel : ObservableObject, IPageViewModel
    {
        ApplicationViewModel _app;
        int _participantId;

        IPageViewModel _previousPage;

        ICommand _saveCommand;
        ICommand _cancelCommand;

        XmlDocument _xmlDoc = null;

        List<ParticipantModel> _allParticipants = new List<ParticipantModel>();
        ParticipantModel _participant;

        string _operation = "";
        string _buttonText = "Save";

        bool _loaded = false;

        public ParticipantViewModel(ApplicationViewModel app)
        {
            _app = app;
            _previousPage = _app.CurrentPageViewModel;
            _operation = "Create";

            LoadParticipants();

            _participant = new ParticipantModel();            
            _participant.ParticipantId = _allParticipants.Count == 0 ? 1 : _allParticipants.Last<ParticipantModel>().ParticipantId + 1;
        }

        public ParticipantViewModel(ApplicationViewModel app, int id, bool isDelete)
        {
            _app = app;
            _participantId = id;

            _previousPage = _app.CurrentPageViewModel;

            if (isDelete)
            {
                _operation = "Delete";
                _buttonText = "Confirm?";
            } else
            {
                _operation = "Edit";
            }

            LoadParticipants();
            IEnumerable<ParticipantModel> p = _allParticipants.Where(x => x.ParticipantId == id);
            _participant = p.FirstOrDefault();
        }

        public string Name => throw new NotImplementedException();

        public string ButtonText
        {
            get { return _buttonText; }
        }

        public int ParticipantId
        {
            get { return _participant.ParticipantId; }
            set
            {
                if (value != _participant.ParticipantId) _participant.ParticipantId = value;
            }
        }

        public string ParticipantName
        {
            get { return _participant.Name; }
            set
            {
                if (value != _participant.Name) _participant.Name = value;
            }
        }

        public string Date
        {
            get { return _participant.Date; }
            set
            {
                if (value != _participant.Date) _participant.Date = value;
            }
        }

        public string Altitude
        {
            get { return _participant.Altitude; }
            set
            {
                if (value != _participant.Altitude) _participant.Altitude = value;
            }
        }

        public int Serie
        {
            get { return _participant.Serie; }
            set
            {
                if (value != _participant.Serie) _participant.Serie = value;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    _cancelCommand = new RelayCommand(param => Cancel());
                }
                return _cancelCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(param => Save(),
                        param => ((_participant.Name != "") &&
                        (_participant.Date != "")));
                }
                return _saveCommand;
            }
        }

        private void Cancel()
        {
            // Just return to previous page and ignores what has been done.
            _app.CurrentPageViewModel = _previousPage;
        }

        private void Save()
        {
            XmlNode xNode = null;

            if (!_loaded) LoadParticipants();

            if (_operation == "Edit" || _operation == "Delete")
            {
                string xpath = "/Participants/Participant[@Id='{0}']";
                xpath = String.Format(xpath, _participant.ParticipantId);
                xNode = _xmlDoc.DocumentElement.SelectSingleNode(xpath);

                // Remove all attributes and children.
                if (xNode != null)
                {
                    xNode.ParentNode.RemoveChild(xNode);
                }
            }

            if (_operation != "Delete")
            {
                xNode = _xmlDoc.CreateNode(XmlNodeType.Element, "Participant", "");

                CreateAttribute(xNode, "Id", _participant.ParticipantId.ToString());
                CreateAttribute(xNode, "Name", _participant.Name);
                CreateAttribute(xNode, "Date", _participant.Date);
                CreateAttribute(xNode, "Altitude", _participant.Altitude);
                CreateAttribute(xNode, "Serie", _participant.Serie.ToString());

                if (_operation == "Create" || _operation == "Edit") _xmlDoc.LastChild.AppendChild(xNode);
            }

            _xmlDoc.Save(@"C:\Users\anton\source\repos\mBESS\mBESS\bin\x64\Debug\Participants.xml");

            if (_operation == "Create" || _operation == "Edit")
            {
                var xDoc = XDocument.Load(@"C:\Users\anton\source\repos\mBESS\mBESS\bin\x64\Debug\Participants.xml");
                var newxDoc = new XElement("Participants", xDoc.Root
                    .Elements()
                    .OrderBy(x => (int)x.Attribute("Id")));

                newxDoc.Save(@"C:\Users\anton\source\repos\mBESS\mBESS\bin\x64\Debug\Participants.xml");
            }

            _app.CurrentPageViewModel = _previousPage;
        }

        private void CreateAttribute(XmlNode xNode, string attName, string attValue)
        {
            XmlAttribute xAtt = _xmlDoc.CreateAttribute(attName);
            xAtt.Value = attValue;
            xNode.Attributes.Append(xAtt);
        }

        private void LoadParticipants()
        {
            if (!_loaded)
            {
                _xmlDoc = new XmlDocument();
                string filename = @"C:\Users\anton\source\repos\mBESS\mBESS\bin\x64\Debug\Participants.xml";

                if (!File.Exists(filename))
                {
                    XmlDeclaration xmlDeclaration = _xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    XmlElement xNode = _xmlDoc.CreateElement("Participants");
                    _xmlDoc.AppendChild(xmlDeclaration);
                    _xmlDoc.AppendChild(xNode);
                    _xmlDoc.Save(filename);
                } else
                {
                    _xmlDoc.Load(filename);
                }

                XmlNodeList nodeList = _xmlDoc.DocumentElement.SelectNodes("/Participants/Participant");

                foreach (XmlNode node in nodeList)
                {
                    _allParticipants.Add(LoadParticipant(node));
                }

                _loaded = true;
            }
        }

        private ParticipantModel LoadParticipant(XmlNode node)
        {
            ParticipantModel p = new ParticipantModel();
            p.ParticipantId= Int32.Parse(node.Attributes["Id"].Value);
            p.Name = node.Attributes["Name"].Value;
            p.Date = node.Attributes["Date"].Value;
            p.Altitude = node.Attributes["Altitude"].Value;
            p.Serie = Int32.Parse(node.Attributes["Serie"].Value);
            return p;
        }

    }
}
