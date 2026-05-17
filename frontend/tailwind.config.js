/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./apps/*/src/**/*.{html,ts}",
    "./libs/*/src/**/*.{html,ts}"
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#e8f4f1',
          100: '#c5e4dc',
          300: '#6cb5a0',
          500: '#006c4f',
          700: '#00513b',
          900: '#00301f',
        },
        accent: {
          500: '#c8a045',
        },
        warn: {
          500: '#b71c1c',
        }
      }
    }
  },
  plugins: [],
}
