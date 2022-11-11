import { FC, useState } from 'react'
import { Document, Page, pdfjs } from 'react-pdf'
import 'react-pdf/dist/esm/Page/AnnotationLayer.css'
import 'react-pdf/dist/esm/Page/TextLayer.css'
import { createStyles, Paper, ScrollArea, Stack } from '@mantine/core'
import { showErrorNotification } from '@Utils/ApiErrorHandler'

pdfjs.GlobalWorkerOptions.workerSrc = `//cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjs.version}/pdf.worker.min.js`

interface PDFViewerProps {
  url?: string
  height?: number | string
}

const useStyles = createStyles((theme, { height }: PDFViewerProps) => ({
  layout: {
    marginLeft: theme.spacing.md,
    marginRight: theme.spacing.md,
    borderRadius: theme.radius.md,

    [theme.fn.largerThan(1100 + theme.spacing.md * 2)]: {
      maxWidth: 900,
      marginLeft: 'auto',
      marginRight: 'auto',
    },

    '& canvas': {
      minWidth: '100% !important',
      maxWidth: '100% !important',
      height: 'auto !important',
      borderRadius: theme.radius.md,
    },

    '& .react-pdf__Page__textContent': {
      display: 'none',
    },
  },

  doc: {
    maxHeight: height,
  },

  paper: {
    marginBottom: theme.spacing.md,
  },
}))

const PDFViewer: FC<PDFViewerProps> = (props) => {
  const [numPages, setNumPages] = useState(0)
  const { classes } = useStyles(props)

  return (
    <ScrollArea className={classes.layout} type="never">
      <Document
        file={props.url}
        className={classes.doc}
        onLoadSuccess={({ numPages }) => {
          setNumPages(numPages)
        }}
        onLoadError={showErrorNotification}
      >
        <Stack>
          {Array.from(new Array(numPages), (el, index) => (
            <Paper className={classes.paper} key={`page_${index + 1}`}>
              <Page width={900} pageNumber={index + 1} renderAnnotationLayer={false} />
            </Paper>
          ))}
        </Stack>
      </Document>
    </ScrollArea>
  )
}

export default PDFViewer
