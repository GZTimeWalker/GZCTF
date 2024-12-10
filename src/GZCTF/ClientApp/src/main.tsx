import { App } from '@App'
import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import ReactDOM from 'react-dom/client'
import { initReactI18next } from 'react-i18next'
import { BrowserRouter } from 'react-router'
import resources from 'virtual:i18next-loader'
import { convertLanguage } from '@Utils/I18n'

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: convertLanguage,
    interpolation: {
      escapeValue: false,
    },
    detection: {
      convertDetectedLanguage: convertLanguage,
    },
    resources: Object.fromEntries(
      Object.entries(resources).map(([lang, res]) => [
        lang,
        {
          translation: res,
        },
      ])
    ),
  })

const app = ReactDOM.createRoot(document.getElementById('root')!)

app.render(
  <BrowserRouter>
    <App />
  </BrowserRouter>
)
