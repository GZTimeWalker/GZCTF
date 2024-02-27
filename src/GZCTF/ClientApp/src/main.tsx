import { App } from '@App'
import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { StrictMode } from 'react'
import ReactDOM from 'react-dom/client'
import { initReactI18next } from 'react-i18next'
import { BrowserRouter as Router } from 'react-router-dom'
import resources from 'virtual:i18next-loader'
import { convertLanguage } from '@Utils/I18n'

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: convertLanguage,
    interpolation: {
      escapeValue: false,
    },
    detection: {
      convertDetectedLanguage: convertLanguage,
    },
  })

ReactDOM.createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Router>
      <App />
    </Router>
  </StrictMode>
)
