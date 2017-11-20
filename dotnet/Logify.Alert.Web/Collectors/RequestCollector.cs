﻿using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;

using DevExpress.Logify.Core;
using System.Collections.Generic;

namespace DevExpress.Logify.Core.Internal {
    class RequestCollector : IInfoCollector {
        readonly HttpRequest request;
        readonly string name;
        readonly IgnorePropertiesInfo ignoreFormNames;
        readonly IgnorePropertiesInfo ignoreHeaders;
        readonly IgnorePropertiesInfo ignoreCookies;
        readonly IgnorePropertiesInfo ignoreServerVariables;
        private ILogger logger;

        public RequestCollector(HttpRequest request, IgnorePropertiesInfoConfig ignoreConfig) : this(request, "request", ignoreConfig) { }

        public RequestCollector(HttpRequest request, string name, IgnorePropertiesInfoConfig ignoreConfig) {
            this.request = request;
            this.name = name;
            this.ignoreFormNames = IgnorePropertiesInfo.FromString(ignoreConfig.IgnoreFormNames);
            this.ignoreHeaders = IgnorePropertiesInfo.FromString(ignoreConfig.IgnoreHeaders);
            this.ignoreCookies = IgnorePropertiesInfo.FromString(ignoreConfig.IgnoreCookies);
            this.ignoreServerVariables = IgnorePropertiesInfo.FromString(ignoreConfig.IgnoreServerVariables);
        }

        public virtual void Process(Exception e, ILogger logger) {
            this.logger = logger;
            try {
                logger.BeginWriteObject(name);
                logger.WriteValue("acceptTypes", request.AcceptTypes);
                this.SerializeBrowserInfo();
                this.SerializeContentInfo();
                this.SerializeFileInfo();
                Utils.SerializeCookieInfo(request.Cookies, this.ignoreCookies, logger);
                Utils.SerializeInfo(request.Form, "form", this.ignoreFormNames, logger);
                Utils.SerializeInfo(request.Headers, "headers", this.ignoreHeaders, logger);
                Utils.SerializeInfo(request.ServerVariables, "serverVariables", this.ignoreServerVariables, logger);
                Utils.SerializeInfo(request.QueryString, "queryString", null, logger);
                logger.WriteValue("applicationPath", request.ApplicationPath);
                logger.WriteValue("httpMethod", request.HttpMethod);
                try { logger.WriteValue("httpRequestBody", (new System.IO.StreamReader(request.InputStream)).ReadToEnd()); } catch { }
                logger.WriteValue("isUserAuthenticated", request.IsAuthenticated);
                logger.WriteValue("isLocalRequest", request.IsLocal);
                logger.WriteValue("isSecureConnection", request.IsSecureConnection);
                logger.WriteValue("requestType", request.RequestType);
                logger.WriteValue("totalBytes", request.TotalBytes);
            }
            finally {
                logger.EndWriteObject(name);
            }
        }

        void SerializeBrowserInfo() {
            try {
                logger.BeginWriteObject("browser");
                HttpBrowserCapabilities browserInfo = request.Browser;
                logger.WriteValue("activeXSupport", browserInfo.ActiveXControls);
                logger.WriteValue("isBeta", browserInfo.Beta);
                logger.WriteValue("userAgent", browserInfo.Browser);
                logger.WriteValue("canVoiceCall", browserInfo.CanInitiateVoiceCall);
                logger.WriteValue("canSendMail", browserInfo.CanSendMail);
                logger.WriteValue("cookiesSupport", browserInfo.Cookies);
                logger.WriteValue("isCrawler", browserInfo.Crawler);
                logger.WriteValue("EcmaScriptVersion", browserInfo.EcmaScriptVersion.ToString());
                logger.WriteValue("frameSupport", browserInfo.Frames);
                logger.WriteValue("gatewayVersion", browserInfo.GatewayVersion.ToString());
                logger.WriteValue("inputType", browserInfo.InputType);
                logger.WriteValue("isMobileDevice", browserInfo.IsMobileDevice);
                logger.WriteValue("javaAppletSupport", browserInfo.JavaApplets);
                if (browserInfo.IsMobileDevice) {
                    logger.WriteValue("mobileDeviceManufacturer", browserInfo.MobileDeviceManufacturer);
                    logger.WriteValue("mobileDeviceModel", browserInfo.MobileDeviceModel);
                }
                logger.WriteValue("platform", browserInfo.Platform);
                this.SerializeScreenInfo();
                logger.WriteValue("type", browserInfo.Type);
                logger.WriteValue("version", browserInfo.Version);
                logger.WriteValue("isWin32", browserInfo.Win32);
            }
            finally {
                logger.EndWriteObject("browser");
            }
        }

        void SerializeScreenInfo() {
            try {
                logger.BeginWriteObject("screen");
                logger.WriteValue("height", request.Browser.ScreenPixelsHeight);
                logger.WriteValue("width", request.Browser.ScreenPixelsWidth);
            }
            finally {
                logger.EndWriteObject("screen");
            }
        }

        void SerializeContentInfo() {
            try {
                logger.BeginWriteObject("content");
                logger.WriteValue("encoding", request.ContentEncoding.EncodingName);
                logger.WriteValue("length", request.ContentLength);
                logger.WriteValue("type", request.ContentType);
            }
            finally {
                logger.EndWriteObject("content");
            }
        }

        void SerializeFileInfo() {
            if (request.Files.Count != 0) {
                try {
                    logger.BeginWriteObject("files");
                    foreach (string key in request.Files.AllKeys) {
                        HttpPostedFile file = request.Files.Get(key);
                        logger.BeginWriteObject(key);
                        logger.WriteValue("name", file.FileName);
                        logger.WriteValue("length", file.ContentLength);
                        logger.WriteValue("type", file.ContentType);
                        logger.EndWriteObject(key);
                    }
                }
                finally {
                    logger.EndWriteObject("files");
                }
            }
        }
    }
    public class IgnorePropertiesInfo {
        public bool IgnoreAll { get; set; }
        public HashSet<string> IgnoreNames { get; set; }

        public bool ShouldIgnore(string name) {
            if (IgnoreAll)
                return true;
            return IgnoreNames != null && IgnoreNames.Contains(name);
        }

        public static IgnorePropertiesInfo FromString(string value) {
            IgnorePropertiesInfo result = new IgnorePropertiesInfo();
            result.IgnoreNames = new HashSet<string>();
            try {
                if (String.IsNullOrEmpty(value))
                    return result;

                string[] names = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (names == null || names.Length <= 0)
                    return result;

                int count = names.Length;
                for (int i = 0; i < count; i++) {
                    string item = names[i].Trim();
                    if (!String.IsNullOrEmpty(item)) {
                        if (item == "*") {
                            result.IgnoreAll = true;
                            break;
                        }
                        if (!result.IgnoreNames.Contains(item))
                            result.IgnoreNames.Add(item);
                    }
                }

                return result;
            }
            catch {
                return result;
            }
        }
    }
}