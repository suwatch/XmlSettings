﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XmlSettings {
    public class Settings : ISettings {
        private readonly XDocument _config;
        private readonly string _path;

        public Settings(string path) {
            _path = path;
            _config = XmlUtility.GetOrCreateDocument("settings", path);
        }

        public string GetValue(string section, string key) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("", "section");
            }

            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("", "key");
            }

            try {
                // Get the section and return null if it doesnt exist
                var sectionElement = _config.Root.Element(section);
                if (sectionElement == null) {
                    return null;
                }

                // Get the add element that matches the key and return null if it doesnt exist
                var element = sectionElement.Elements("add").Where(s => s.GetOptionalAttributeValue("key") == key).FirstOrDefault();
                if (element == null) {
                    return null;
                }

                // Return the optional value which if not there will be null;
                return element.GetOptionalAttributeValue("value");
            }
            catch (Exception e) {
                throw new InvalidOperationException("Unable to parse settings file", e);
            }
        }

        public IList<KeyValuePair<string, string>> GetValues(string section) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("", "section");
            }

            try {
                var sectionElement = _config.Root.Element(section);
                if (sectionElement == null) {
                    return null;
                }

                var kvps = new List<KeyValuePair<string, string>>();
                foreach (var e in sectionElement.Elements("add")) {
                    var key = e.GetOptionalAttributeValue("key");
                    var value = e.GetOptionalAttributeValue("value");
                    if (!String.IsNullOrEmpty(key) && value != null) {
                        kvps.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
                return kvps.AsReadOnly();
            }
            catch (Exception e) {
                throw new InvalidOperationException("Unable to parse settings file.", e);
            }
        }

        public void SetValue(string section, string key, string value) {
            SetValueInternal(section, key, value);
            Save(_config);
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values) {
            if (values == null) {
                throw new ArgumentNullException("values");
            }

            foreach (var kvp in values) {
                SetValueInternal(section, kvp.Key, kvp.Value);
            }
            Save(_config);
        }

        private void SetValueInternal(string section, string key, string value) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("", "section");
            }
            
            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("", "key");
            }

            if (value == null) {
                throw new ArgumentNullException("value");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null) {
                sectionElement = new XElement(section);
                _config.Root.Add(sectionElement);
            }

            foreach (var e in sectionElement.Elements("add")) {
                var tempKey = e.GetOptionalAttributeValue("key");

                if (tempKey == key) {
                    e.SetAttributeValue("value", value);
                    Save(_config);
                    return;
                }
            }

            var addElement = new XElement("add");
            addElement.SetAttributeValue("key", key);
            addElement.SetAttributeValue("value", value);
            sectionElement.Add(addElement);
        }

        public bool DeleteValue(string section, string key) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("", "section");
            }

            if (String.IsNullOrEmpty(key)) {
                throw new ArgumentException("", "key");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null) {
                return false;
            }

            XElement elementToDelete = null;
            foreach (var e in sectionElement.Elements("add")) {
                if (e.GetOptionalAttributeValue("key") == key) {
                    elementToDelete = e;
                    break;
                }
            }
            if (elementToDelete == null) {
                return false;
            }

            elementToDelete.Remove();
            Save(_config);
            return true;

        }

        public bool DeleteSection(string section) {
            if (String.IsNullOrEmpty(section)) {
                throw new ArgumentException("", "section");
            }

            var sectionElement = _config.Root.Element(section);
            if (sectionElement == null) {
                return false;
            }

            sectionElement.Remove();
            Save(_config);
            return true;
        }

        private void Save(XDocument document) {
            document.Save(_path);
        }
    }
}
