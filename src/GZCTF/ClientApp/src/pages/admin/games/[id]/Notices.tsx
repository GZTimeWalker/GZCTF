import { Button, Center, Divider, ScrollArea, Stack, Text, Title } from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { InlineMarkdown } from '@Components/MarkdownRenderer'
import { GameNoticeEditCard } from '@Components/admin/GameNoticeEditCard'
import { GameNoticeEditModal } from '@Components/admin/GameNoticeEditModal'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { OnceSWRConfig } from '@Hooks/useConfig'
import api, { GameNotice } from '@Api'

const GameNoticeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data: gameNotices, mutate } = api.edit.useEditGetGameNotices(numId, OnceSWRConfig)

  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeGameNotice, setActiveGameNotice] = useState<GameNotice | null>(null)

  const { t } = useTranslation()

  // delete
  const modals = useModals()
  const onDeleteGameNotice = (gameNotice: GameNotice) => {
    modals.openConfirmModal({
      title: t('admin.button.notices.delete'),
      children: (
        <Stack>
          <Text> {t('admin.content.games.notices.delete')}</Text>
          <Divider />
          <InlineMarkdown source={gameNotice.values.at(-1) || ''} />
        </Stack>
      ),
      onConfirm: () => onConfirmDelete(gameNotice),
      confirmProps: { color: 'red' },
    })
  }
  const onConfirmDelete = async (gameNotice: GameNotice) => {
    try {
      await api.edit.editDeleteGameNotice(numId, gameNotice.id)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.notices.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate(gameNotices?.filter((t) => t.id !== gameNotice.id) ?? [])
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  return (
    <WithGameEditTab
      headProps={{ justify: 'space-between' }}
      contentPos="right"
      head={
        <Button
          leftSection={<Icon path={mdiPlus} size={1} />}
          onClick={() => {
            setActiveGameNotice(null)
            setIsEditModalOpen(true)
          }}
        >
          {t('admin.button.notices.new')}
        </Button>
      }
    >
      <ScrollArea pos="relative" h="calc(100vh - 180px)" offsetScrollbars>
        {!gameNotices || gameNotices?.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack gap={0}>
              <Title order={2}>{t('admin.content.games.notices.empty.title')}</Title>
              <Text>{t('admin.content.games.notices.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <Stack gap="lg" align="center" m="2%">
            {gameNotices.map((gameNotice) => (
              <GameNoticeEditCard
                w="95%"
                key={gameNotice.id}
                gameNotice={gameNotice}
                onDelete={() => {
                  onDeleteGameNotice(gameNotice)
                }}
                onEdit={() => {
                  setActiveGameNotice(gameNotice)
                  setIsEditModalOpen(true)
                }}
              />
            ))}
          </Stack>
        )}
      </ScrollArea>
      <GameNoticeEditModal
        size="30%"
        title={activeGameNotice ? t('admin.button.notices.edit') : t('admin.button.notices.new')}
        opened={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        gameNotice={activeGameNotice}
        mutateGameNotice={(gameNotice: GameNotice) => {
          mutate([gameNotice, ...(gameNotices?.filter((n) => n.id !== gameNotice.id) ?? [])])
        }}
      />
    </WithGameEditTab>
  )
}

export default GameNoticeEdit
