import { useDocumentTitle } from '@mantine/hooks'
import { useConfig } from './useConfig'

export const usePageTitle = (title?: string) => {
  const { config, error } = useConfig()

  const platfrom = error ? 'GZ::CTF' : `${config?.title}::CTF`

  useDocumentTitle(
    typeof title === 'string' && title.trim().length > 0 ? `${title} - ${platfrom}` : platfrom
  )
}
