import { Button, Center, Group, ScrollArea, Stack, Text, Title } from '@mantine/core'
import { mdiFolderDownloadOutline, mdiKeyboardBackspace } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { ErrorBoundary } from 'react-error-boundary'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import PDFViewer from '@Components/admin/PDFViewer'
import TeamWriteupCard from '@Components/admin/TeamWriteupCard'
import WithGameTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { OnceSWRConfig } from '@Utils/useConfig'
import api, { WriteupInfoModel } from '@Api'

const GameWriteups: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const navigate = useNavigate()
  const [selected, setSelected] = useState<WriteupInfoModel>()

  const { data: writeups } = api.admin.useAdminWriteups(numId, OnceSWRConfig)
  const { t } = useTranslation()

  useEffect(() => {
    if (writeups?.length && !selected) {
      setSelected(writeups[0])
    }
  }, [writeups])

  return (
    <WithGameTab
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            {t('admin.button.back')}
          </Button>

          <Group grow miw="15rem" maw="15rem" position="apart">
            <Button
              fullWidth
              leftIcon={<Icon path={mdiFolderDownloadOutline} size={1} />}
              onClick={() => window.open(`/api/admin/writeups/${id}/all`, '_blank')}
            >
              {t('admin.button.writeups.download_all')}
            </Button>
          </Group>
        </>
      }
    >
      {!writeups?.length || !selected ? (
        <Center mih="calc(100vh - 180px)">
          <Stack spacing={0}>
            <Title order={2}>{t('admin.content.games.writeups.empty.title')}</Title>
            <Text>{t('admin.content.games.writeups.empty.description')}</Text>
          </Stack>
        </Center>
      ) : (
        <Group noWrap align="flex-start" position="apart">
          <Stack pos="relative" mt="-3rem" w="calc(100% - 16rem)">
            <ErrorBoundary
              fallback={
                <Center mih="calc(100vh - 110px)">
                  <Text>{t('admin.content.games.writeups.pdf_fallback')}</Text>
                </Center>
              }
              onError={(e) => showErrorNotification(e, t)}
            >
              <PDFViewer url={selected?.url} height="calc(100vh - 110px)" />
            </ErrorBoundary>
          </Stack>
          <ScrollArea miw="15rem" maw="15rem" h="calc(100vh - 110px - 3rem)" type="never">
            <Stack>
              {writeups?.map((writeup) => (
                <TeamWriteupCard
                  key={writeup.id}
                  writeup={writeup}
                  selected={selected?.id === writeup.id}
                  onClick={() => setSelected(writeup)}
                >
                  <Text>{writeup.team?.name}</Text>
                </TeamWriteupCard>
              ))}
            </Stack>
          </ScrollArea>
        </Group>
      )}
    </WithGameTab>
  )
}

export default GameWriteups
