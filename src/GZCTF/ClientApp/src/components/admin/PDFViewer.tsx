import { Box, Center, Paper, ScrollArea, Stack, Text, em } from '@mantine/core'
import { FC, useRef, useState } from 'react'
import { ErrorBoundary } from 'react-error-boundary'
import { useTranslation } from 'react-i18next'
import { Document, Page, pdfjs } from 'react-pdf'
import 'react-pdf/dist/esm/Page/AnnotationLayer.css'
import 'react-pdf/dist/esm/Page/TextLayer.css'
import { showErrorNotification } from '@Utils/ApiHelper'
import classes from '@Styles/PDFViewer.module.css'

pdfjs.GlobalWorkerOptions.workerSrc = new URL(
  'pdfjs-dist/build/pdf.worker.min.mjs',
  import.meta.url
).toString()

interface PDFViewerProps {
  url?: string
  height?: number | string
}

const PDFViewer: FC<PDFViewerProps> = ({ url, height }) => {
  const [numPages, setNumPages] = useState(0)
  const { t } = useTranslation()

  const h = height ? em(height) : 'calc(100vh - 110px)'
  const ref = useRef<HTMLDivElement>(null)

  const renderWidth = Math.round(2480 / 3)
  const pageWidth = ref.current?.offsetWidth ?? renderWidth
  const ratio = pageWidth / renderWidth

  return (
    <ErrorBoundary
      fallback={
        <Center mih={h}>
          <Text>{t('admin.content.games.writeups.pdf_fallback')}</Text>
        </Center>
      }
      onError={(e) => showErrorNotification(e, t)}
    >
      <Box
        className={classes.box}
        __vars={{
          '--pdf-height': h,
        }}
      >
        <ScrollArea h={h} className={classes.layout} type="never">
          <Document
            file={url}
            className={classes.doc}
            onLoadSuccess={({ numPages }) => {
              setNumPages(numPages)
            }}
            onLoadError={(e) => showErrorNotification(e, t)}
          >
            <Stack ref={ref}>
              {Array.from(Array.from({ length: numPages }), (_, index) => (
                <Paper className={classes.paper} key={`page_${index + 1}`}>
                  <Page
                    width={renderWidth}
                    scale={ratio}
                    pageNumber={index + 1}
                    renderAnnotationLayer={false}
                  />
                </Paper>
              ))}
            </Stack>
          </Document>
        </ScrollArea>
      </Box>
    </ErrorBoundary>
  )
}

export default PDFViewer
