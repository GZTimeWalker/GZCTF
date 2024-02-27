import { App } from '@App'
import { LanguageMap, SupportedLanguages, defaultLanguage } from '@Utils/I18n'
import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { StrictMode } from 'react'
import ReactDOM from 'react-dom/client'
import { initReactI18next } from 'react-i18next'
import { BrowserRouter as Router } from 'react-router-dom'
import resources from 'virtual:i18next-loader'

const convertLanguage = (language: string): SupportedLanguages => {
  const normalizeLanguage = (language: string) => {
    let sliceIndex = language.indexOf('-')
    if (sliceIndex === -1) sliceIndex = language.indexOf('_')
    if (sliceIndex === -1) sliceIndex = language.length
    return language.toLowerCase().slice(0, sliceIndex)
  }
  const normalizedLanguage = normalizeLanguage(language)

  const matchedLanguage = Object.keys(LanguageMap).filter((lang) => normalizeLanguage(lang) === normalizedLanguage)
  if (matchedLanguage.length > 0) {
    return matchedLanguage[0] as SupportedLanguages
  }

  return defaultLanguage
}

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: defaultLanguage,
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
