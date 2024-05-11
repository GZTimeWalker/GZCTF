import { Center, Paper, ScrollArea, Stack, Text, em } from '@mantine/core'
import { FC, useState } from 'react'
import { ErrorBoundary } from 'react-error-boundary'
import { useTranslation } from 'react-i18next'
import { Document, Page, pdfjs } from 'react-pdf'
import 'react-pdf/dist/esm/Page/AnnotationLayer.css'
import 'react-pdf/dist/esm/Page/TextLayer.css'
import { showErrorNotification } from '@Utils/ApiHelper'
import classes from '@Styles/PDFViewer.module.css'

pdfjs.GlobalWorkerOptions.workerSrc = new URL(
  'pdfjs-dist/build/pdf.worker.js',
  import.meta.url
).href

interface PDFViewerProps {
  url?: string
  height?: number | string
}

const MIN_HEIGHT = 'calc(100vh - 110px)'

const PDFViewer: FC<PDFViewerProps> = ({ url, height }) => {
  const [numPages, setNumPages] = useState(0)
  const { t } = useTranslation()
  const h = height ? em(height) : MIN_HEIGHT

  return (
    <ErrorBoundary
      fallback={
        <Center mih={h}>
          <Text>{t('admin.content.games.writeups.pdf_fallback')}</Text>
        </Center>
      }
      onError={(e) => showErrorNotification(e, t)}
    >
      <ScrollArea
        __vars={{
          '--pdf-height': h,
        }}
        h={h}
        className={classes.layout}
        type="never"
      >
        <Document
          file={url}
          className={classes.doc}
          onLoadSuccess={({ numPages }) => {
            setNumPages(numPages)
          }}
          onLoadError={(e) => showErrorNotification(e, t)}
        >
          <Stack>
            {Array.from(new Array(numPages), (_, index) => (
              <Paper className={classes.paper} key={`page_${index + 1}`}>
                <Page width={800} pageNumber={index + 1} renderAnnotationLayer={false} />
              </Paper>
            ))}
          </Stack>
        </Document>
      </ScrollArea>
    </ErrorBoundary>
  )
}

export default PDFViewer
