
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

        async markup({ filename }) {

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

            if (!existsSync(csharpFile)) {
                return;
            }

            registerCsharpDependency(csharpFile);

            try {
                const code = await compileComponent(
                    filename,
                    options
                );

                return {
                    code,
                    dependencies: [csharpFile]
                };

            } catch (error) {
                const message = error instanceof Error
                    ? error.message
                    : String(error);

                throw new Error(
                    `SvelteSharp failed in ${filename}:\n${message}`
                );
            }
        }
    };
}
