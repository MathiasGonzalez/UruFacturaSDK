import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
  site: 'https://mathiasgonzalez.github.io',
  base: '/UruFacturaSDK',
  integrations: [
    starlight({
      title: 'UruFactura SDK',
      description: 'La vía rápida hacia la Facturación Electrónica en Uruguay. SDK .NET open-source para integración con DGI.',
      logo: {
        light: './src/assets/logo-light.svg',
        dark: './src/assets/logo-dark.svg',
        replacesTitle: false,
      },
      social: [
        { icon: 'github', label: 'GitHub', href: 'https://github.com/MathiasGonzalez/UruFacturaSDK' },
        { icon: 'external', label: 'NuGet', href: 'https://www.nuget.org/packages/UruFacturaSDK/' },
      ],
      editLink: {
        baseUrl: 'https://github.com/MathiasGonzalez/UruFacturaSDK/edit/main/landing/',
      },
      customCss: ['./src/styles/custom.css'],
      sidebar: [
        {
          label: '🚀 Inicio',
          link: '/',
        },
        {
          label: 'Primeros Pasos',
          autogenerate: { directory: 'getting-started' },
        },
        {
          label: 'Guías',
          autogenerate: { directory: 'guides' },
        },
        {
          label: 'Migración',
          autogenerate: { directory: 'migration' },
        },
        {
          label: 'Referencia',
          autogenerate: { directory: 'reference' },
        },
      ],
      head: [
        {
          tag: 'meta',
          attrs: {
            property: 'og:image',
            content: 'https://mathiasgonzalez.github.io/UruFacturaSDK/og-image.png',
          },
        },
      ],
    }),
  ],
});
