import { App } from '@App'
import { StrictMode } from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter as Router } from 'react-router-dom'
import i18n from "i18next"
import { initReactI18next } from "react-i18next"
import LanguageDetector from 'i18next-browser-languagedetector';
import { resources } from './utils/AppTrans'

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: "zh-CN",
    interpolation: {
      escapeValue: false
    },
    detection: {
      convertDetectedLanguage: 'Iso15897'
    }
  });

ReactDOM.createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Router>
      <App />
    </Router>
  </StrictMode>
)
