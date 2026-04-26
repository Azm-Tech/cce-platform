// NOTE: @hey-api/openapi-ts 0.61.2 has a bug where the array-of-configs form fails to
// parse the `input` field via `defineConfig`. We invoke the CLI twice with explicit
// --input/--output flags from project.json's `generate` target instead — see the script
// invocations there. This file is kept for reference + future single-spec usage.
import { defineConfig } from '@hey-api/openapi-ts';
import { resolve } from 'node:path';

const repoRoot = resolve(__dirname, '../../..');

export default defineConfig({
  input: resolve(repoRoot, 'contracts/openapi.external.json'),
  output: resolve(__dirname, 'src/lib/generated/external'),
  plugins: ['@hey-api/typescript', '@hey-api/sdk', '@hey-api/client-fetch'],
});
