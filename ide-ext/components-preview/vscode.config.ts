import { defineConfig } from '@rsbuild/core';
import { pluginReact } from '@rsbuild/plugin-react';
import { pluginSass } from '@rsbuild/plugin-sass';

export default defineConfig({
  plugins: [pluginReact(), pluginSass()],
  output: {
    distPath: "../vs-code/discord-preview",
    assetPrefix: "EXT_PATH_PREFIX",
    inlineScripts: true,
    inlineStyles: true,
    minify: true
  }
});
