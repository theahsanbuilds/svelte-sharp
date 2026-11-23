
import { existsSync } from 'node:fs';
import type { PreprocessorGroup } from 'svelte/compiler';

import { compileComponent } from './compiler-runner.js';
import { registerCsharpDependency } from './watcher.js';
import type { SvelteSharpOptions } from './types.js';

export function svelteSharpPreprocessor(
    options: SvelteSharpOptions = {}
): PreprocessorGroup {

    return {
        name: 'svelte-sharp-preprocessor',

        async markup({ content, filename }) {

            if (!filename?.endsWith('.svelte')) {
                return;
            }

            if (
                filename.includes('/.svelte-kit/') ||
                filename.includes('\\.svelte-kit\\')
            ) {
                return;
            }

            const csharpFile = `${filename}.cs`;

            const hasCsharpFile = existsSync(csharpFile);

            const hasInlineCode = /@code\s*\{/.test(content);

            if (!hasCsharpFile && !hasInlineCode) {
                return;
            }

            if (hasCsharpFile) {
                registerCsharpDependency(csharpFile);
            }

            try {
                const code = await compileComponent(
                    filename,
                    options,
                    content
                );

                return {
                    code,
                    dependencies: hasCsharpFile
                        ? [csharpFile]
                        : []
                };

            } 
            
           
catch (error) {
    const message = error instanceof Error
        ? error.message
        : String(error);

    if (process.env.NODE_ENV === 'production') {
        console.error(
            `[SvelteSharp] Compilation failed in ${filename}:\n${message}`
        );

        return;
    }

    throw new Error(
        `SvelteSharp failed in ${filename}:\n${message}`
    );
}

        }
    };
}
