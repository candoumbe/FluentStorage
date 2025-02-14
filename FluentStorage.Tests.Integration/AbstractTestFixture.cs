﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using LogMagic;

namespace FluentStorage.Tests.Integration {
	public class AbstractTestFixture : IDisposable {
		private const string TestDirPrefix = "INTEGRATION-TEST-";
		private const string TestStorageDirName = "INTEGRATION-STATE";
		private DirectoryInfo _testDir;
		private DirectoryInfo _buildDir;
		private DirectoryInfo _testStorageDir;

		static AbstractTestFixture() {
			ServicePointManager.ServerCertificateValidationCallback += CertificateValidationCallback;
			L.Config.WriteTo.Trace();
		}

		private static bool CertificateValidationCallback(
		   object sender,
		   X509Certificate certificate,
		   X509Chain chain,
		   SslPolicyErrors sslPolicyErrors) {
			return true;
		}


		/// <summary>
		/// Isolated directory will be created for every test only when needed, and destroyed automagicaly
		/// </summary>
		protected DirectoryInfo TestDir {
			get {
				if (_testDir == null) {
					Cleanup();

					string testDir = Path.Combine(BuildDir.FullName, TestDirPrefix + Guid.NewGuid());
					Directory.CreateDirectory(testDir);
					_testDir = new DirectoryInfo(testDir);
				}
				return _testDir;
			}
		}

		protected void Cleanup() {
			_testDir = null;
		}

		protected DirectoryInfo BuildDir {
			get {
				return _buildDir ??
					   (_buildDir = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory);
			}
		}

		private DirectoryInfo TestStorageDir {
			get {
				if (_testStorageDir == null) {
					string dirPath = Path.Combine(BuildDir.FullName, TestStorageDirName);
					if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
					_testStorageDir = new DirectoryInfo(dirPath);
				}

				return _testStorageDir;
			}
		}

		private string GetCallingMethodName() {
			var stackTrace = new StackTrace(0, true);

			/*
			 * frames:
			 * 0 - GetCallingMethodName
			 * 1 - get_TestMethodStorage (caller)
			 * 2 - actual test method
			 */

			StackFrame[] frames = stackTrace.GetFrames();
			if (frames == null || frames.Length < 2) throw new ApplicationException("cannot get second stack frame");
			StackFrame testMethodFrame = frames[2];

			MethodBase testMethod = testMethodFrame.GetMethod();

			string testMethodName = testMethod.Name;
			Type testClassType = testMethod.DeclaringType;
			if (testClassType == null) throw new ApplicationException("cannot get test class");

			return string.Format("{0}.{1}", testClassType.Namespace, testMethodName);
		}

		public virtual void Dispose() {

		}


		#region [ Assert Helpers ]

		#endregion
	}
}
