import { Button, Center, Group, ScrollArea, Select, Stack, Text, Title } from '@mantine/core'
import { mdiFolderDownloadOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { PDFViewer } from '@Components/admin/PDFViewer'
import { TeamWriteupCard } from '@Components/admin/TeamWriteupCard'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { OnceSWRConfig } from '@Hooks/useConfig'
import api, { WriteupInfo } from '@Api'

const GameWriteups: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [selected, setSelected] = useState<WriteupInfo>()
  const [selectedDivision, setSelectedDivision] = useState<string>('')

  const { data } = api.admin.useAdminWriteups(numId, OnceSWRConfig)
  const { t } = useTranslation()

  const writeups = useMemo(() => data?.writeups ?? [], [data?.writeups])

  const { divisions, divisionOptions } = useMemo(() => {
    const rawDivisions = data?.divisions ?? {}
    const divisions = Object.fromEntries(Object.entries(rawDivisions).map(([k, v]) => [parseInt(k), v]))
    const divisionOptions = [
      { value: '', label: t('game.label.score_table.all_teams') },
      ...Object.entries(divisions).map(([id, name]) => ({ value: id.toString(), label: name })),
    ]
    return { divisions, divisionOptions }
  }, [data?.divisions, t])

  const filteredWriteups = useMemo(() => {
    const div = parseInt(selectedDivision)
    return selectedDivision ? writeups.filter((w) => w.divisionId === div) : writeups
  }, [selectedDivision, writeups])

  useEffect(() => {
    if (filteredWriteups?.length && (!selected || !filteredWriteups.some((w) => w.id === selected.id))) {
      setSelected(filteredWriteups[0])
    }
  }, [filteredWriteups, selected])

  return (
    <WithGameEditTab
      headProps={{ justify: 'apart' }}
      contentPos="right"
      head={
        <Button
          fullWidth
          w="15rem"
          leftSection={<Icon path={mdiFolderDownloadOutline} size={1} />}
          onClick={() => window.open(`/api/admin/writeups/${id}/all`, '_blank')}
        >
          {t('admin.button.writeups.download_all')}
        </Button>
      }
    >
      <Group wrap="nowrap" align="flex-start" justify="space-between">
        {!filteredWriteups?.length || !selected ? (
          <Center w="100%" mih="calc(100vh - 180px)">
            <Stack gap={0}>
              <Title order={2}>{t('admin.content.games.writeups.empty.title')}</Title>
              <Text>{t('admin.content.games.writeups.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <Stack pos="relative" mt="-3rem" w="calc(100% - 120px)">
            <PDFViewer url={selected?.url} height="calc(100vh - 110px)" />
          </Stack>
        )}
        <Stack gap="sm" miw="15rem" maw="15rem" h="calc(100vh - 110px - 3rem)">
          <Select
            data={divisionOptions}
            value={selectedDivision}
            onChange={(value) => setSelectedDivision(value ?? '')}
          />
          <ScrollArea type="never" style={{ flex: 1 }}>
            <Stack gap="sm">
              {filteredWriteups?.map((writeup) => (
                <TeamWriteupCard
                  key={writeup.id}
                  writeup={writeup}
                  selected={selected?.id === writeup.id}
                  onClick={() => setSelected(writeup)}
                  divisionName={writeup.divisionId ? divisions[writeup.divisionId] : undefined}
                />
              ))}
            </Stack>
          </ScrollArea>
        </Stack>
      </Group>
    </WithGameEditTab>
  )
}

export default GameWriteups
