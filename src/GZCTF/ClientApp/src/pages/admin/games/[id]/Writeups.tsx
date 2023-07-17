import React, { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button, Center, Group, Stack, Title, Text, ScrollArea } from '@mantine/core'
import { mdiFolderDownloadOutline, mdiKeyboardBackspace } from '@mdi/js'
import { Icon } from '@mdi/react'
import PDFViewer from '@Components/admin/PDFViewer'
import TeamWriteupCard from '@Components/admin/TeamWriteupCard'
import WithGameTab from '@Components/admin/WithGameEditTab'
import { OnceSWRConfig } from '@Utils/useConfig'
import api, { WriteupInfoModel } from '@Api'

const GameWriteups: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const navigate = useNavigate()
  const [selected, setSelected] = useState<WriteupInfoModel>()

  const { data: writeups } = api.admin.useAdminWriteups(numId, OnceSWRConfig)

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
            返回上级
          </Button>

          <Group grow miw="15rem" maw="15rem" position="apart">
            <Button
              fullWidth
              leftIcon={<Icon path={mdiFolderDownloadOutline} size={1} />}
              onClick={() => window.open(`/api/admin/writeups/${id}/all`)}
            >
              下载全部 Writeup
            </Button>
          </Group>
        </>
      }
    >
      {!writeups?.length || !selected ? (
        <Center mih="calc(100vh - 180px)">
          <Stack spacing={0}>
            <Title order={2}>Ouch! 这个还没有队伍提交 Writeup</Title>
            <Text>新提交的 Writeup 会显示在这里</Text>
          </Stack>
        </Center>
      ) : (
        <Group noWrap align="flex-start" position="apart">
          <Stack pos="relative" mt="-3rem" w="calc(100% - 16rem)">
            <PDFViewer url={selected.url} height="calc(100vh - 110px)" />
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
