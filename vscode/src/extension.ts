
import * as vscode from 'vscode';

import { spawn, ChildProcessWithoutNullStreams } from 'node:child_process';
import path from 'node:path';

interface CompilerDiagnostic {
    FilePath: string;
    Message: string;
    Severity: string;
    Code: string;
    Line: number;
    Column: number;
}

interface CompilerResult {
    diagnostics: CompilerDiagnostic[];
}

export function activate(context: vscode.ExtensionContext) {

    const output = vscode.window.createOutputChannel('SvelteSharp');

    const diagnosticCollection =
        vscode.languages.createDiagnosticCollection('svelte-sharp');

    context.subscriptions.push(
        output,
        diagnosticCollection
    );

    // Compiler project is one directory above the vscode folder.
    const repositoryRoot = path.resolve(
        context.extensionPath,
        '..'
    );

    const compilerProject = path.join(
        repositoryRoot,
        'compiler',
        'SvelteSharp.Compiler',
        'SvelteSharp.Compiler.csproj'
    );

    const timers = new Map<string, ReturnType<typeof setTimeout>>();

    const runningProcesses =
        new Map<string, ChildProcessWithoutNullStreams>();

    function scheduleDiagnostics(svelteFile: string) {

        const existingTimer = timers.get(svelteFile);

        if (existingTimer) {
            clearTimeout(existingTimer);
        }

        const timer = setTimeout(() => {
            timers.delete(svelteFile);
            runDiagnostics(svelteFile);
        }, 350);

        timers.set(svelteFile, timer);
    }

    function runDiagnostics(svelteFile: string) {

        const existingProcess =
            runningProcesses.get(svelteFile);

        existingProcess?.kill();

        const compiler = spawn(
            'dotnet',
            [
                'run',
                '--no-build',
                '--project',
                compilerProject,
                '--',
                '--diagnostics',
                svelteFile
            ],
            {
                cwd: repositoryRoot,
                windowsHide: true
            }
        );

        runningProcesses.set(svelteFile, compiler);

        let stdout = '';
        let stderr = '';

        compiler.stdout.on('data', data => {
            stdout += data.toString();
        });

        compiler.stderr.on('data', data => {
            stderr += data.toString();
        });

        compiler.on('error', error => {

            output.appendLine(
                `Compiler process error: ${error.message}`
            );

            runningProcesses.delete(svelteFile);
        });

        compiler.on('close', () => {

            if (runningProcesses.get(svelteFile) !== compiler) {
                return;
            }

            runningProcesses.delete(svelteFile);

            try {

                const result =
                    JSON.parse(stdout) as CompilerResult;

                const grouped =
                    new Map<string, vscode.Diagnostic[]>();

                for (const item of result.diagnostics ?? []) {

                    const uri = vscode.Uri.file(item.FilePath);

                    const line = Math.max(0, item.Line - 1);
                    const column = Math.max(0, item.Column - 1);

                    const range = new vscode.Range(
                        line,
                        column,
                        line,
                        column + 1
                    );

                    const severity =
                        item.Severity.toLowerCase() === 'warning'
                            ? vscode.DiagnosticSeverity.Warning
                            : vscode.DiagnosticSeverity.Error;

                    const diagnostic = new vscode.Diagnostic(
                        range,
                        item.Message,
                        severity
                    );

                    diagnostic.source = 'SvelteSharp';
                    diagnostic.code = item.Code;

                    const filePath = uri.fsPath;

                    if (!grouped.has(filePath)) {
                        grouped.set(filePath, []);
                    }

                    grouped.get(filePath)!.push(diagnostic);
                }

                // Clear previous diagnostics for this component
                // and its companion C# file.
                diagnosticCollection.delete(
                    vscode.Uri.file(svelteFile)
                );

                diagnosticCollection.delete(
                    vscode.Uri.file(svelteFile + '.cs')
                );

                for (const [filePath, diagnostics] of grouped) {

                    diagnosticCollection.set(
                        vscode.Uri.file(filePath),
                        diagnostics
                    );
                }

                output.appendLine(
                    `Checked: ${svelteFile} (${result.diagnostics?.length ?? 0} diagnostics)`
                );

            } catch {

                output.appendLine(
                    `Could not read compiler output for: ${svelteFile}`
                );

                if (stderr.trim()) {
                    output.appendLine(stderr);
                }

                if (stdout.trim()) {
                    output.appendLine(stdout);
                }
            }
        });
    }

    // Preserve the existing information command.
    const command = vscode.commands.registerCommand(
        'svelte-sharp.showInfo',
        () => {

            const editor = vscode.window.activeTextEditor;

            if (!editor ||
                editor.document.languageId !== 'svelte') {

                vscode.window.showWarningMessage(
                    'Open a Svelte component first.'
                );

                return;
            }

            output.appendLine(
                `Active component: ${editor.document.fileName}`
            );

            vscode.window.showInformationMessage(
                'SvelteSharp extension is working!'
            );

            output.show();
        }
    );

    context.subscriptions.push(command);

    // Run diagnostics when a Svelte document opens.
    for (const document of vscode.workspace.textDocuments) {

        if (document.languageId === 'svelte') {
            scheduleDiagnostics(document.uri.fsPath);
        }
    }

    context.subscriptions.push(
        vscode.workspace.onDidOpenTextDocument(document => {

            if (document.languageId === 'svelte') {
                scheduleDiagnostics(document.uri.fsPath);
            }
        })
    );

    // Run diagnostics after saving a Svelte component.
    context.subscriptions.push(
        vscode.workspace.onDidSaveTextDocument(document => {

            if (document.languageId === 'svelte') {
                scheduleDiagnostics(document.uri.fsPath);
            }
        })
    );

    // Watch companion .svelte.cs files.
    const csharpWatcher =
        vscode.workspace.createFileSystemWatcher('**/*.svelte.cs');

    context.subscriptions.push(
        csharpWatcher,

        csharpWatcher.onDidChange(uri => {
            scheduleDiagnostics(uri.fsPath.slice(0, -3));
        }),

        csharpWatcher.onDidCreate(uri => {
            scheduleDiagnostics(uri.fsPath.slice(0, -3));
        }),

        csharpWatcher.onDidDelete(uri => {
            const svelteFile = uri.fsPath.slice(0, -3);

            diagnosticCollection.delete(
                vscode.Uri.file(svelteFile + '.cs')
            );

            scheduleDiagnostics(svelteFile);
        })
    );

    output.appendLine('SvelteSharp diagnostics activated.');
}

export function deactivate() {}
