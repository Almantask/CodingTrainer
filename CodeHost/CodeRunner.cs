﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

// References:
//  Permissions, partially-trusted code and fully-trusted assemblies:
//    https://docs.microsoft.com/en-us/dotnet/framework/misc/how-to-run-partially-trusted-code-in-a-sandbox
//  Compiling and running code:
//    https://blog.jayway.com/2015/05/09/using-roslyn-to-build-a-simple-c-interactive-script-engine/
//
// Be sure to install Microsoft.CodeAnalysis.CSharp.Scripting package

namespace CodingTrainer.CSharpRunner.CodeHost
{
    public partial class CodeRunner : ICodeRunner
    {
        public event ConsoleWriteEventHandler ConsoleWrite;
        private TextWriter consoleInWriter;
        private IExceptionLogger exceptionLogger;

        public CodeRunner()
        { }
        public CodeRunner(IExceptionLogger exceptionLogger)
        {
            this.exceptionLogger = exceptionLogger;
        }

        public async Task RunCode(string code)
        {
            try
            {
                var compilation = await Compiler.GetCompilation(code);
                var (result, pdb) = await Compiler.Emit(compilation);

                using (var consoleInStream = new BlockingMemoryStream())
                using (consoleInWriter = TextWriter.Synchronized(new StreamWriter(consoleInStream)))
                {
                    var sandboxMgr = new SandboxManager();
                    sandboxMgr.ConsoleWrite += OnConsoleWrite;
                    sandboxMgr.RunInSandbox(result, pdb, consoleInStream);
                }
            }
            catch (Exception e) when (!(e is AggregateException || e is CompilationErrorException))
            {
                if (exceptionLogger != null)
                {
                    await exceptionLogger.LogException(e, code);
                }
                throw;
            }
            finally
            {
                consoleInWriter = null;
            }
        }

        // Put some text into the console input stream
        public void ConsoleIn(string text)
        {
            if (consoleInWriter == null)
                throw new InvalidOperationException("Can't send input text to the console until the code is running");

            consoleInWriter.WriteLine(text);
            consoleInWriter.Flush();
        }

        // Handle console writes to the output stream
        public void OnConsoleWrite(object sender, ConsoleWriteEventArgs e)
        {
            ConsoleWrite?.Invoke(this, e);
        }
    }
}
