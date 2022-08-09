import { useDocumentTitle } from '@mantine/hooks'

export const usePageTitle = (title?: string) =>
  useDocumentTitle(
    typeof title === 'string' && title.trim().length > 0 ? `${title} - GZ::CTF` : 'GZ::CTF'
  )
