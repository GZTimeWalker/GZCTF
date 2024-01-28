import { Button, Center, Group, ScrollArea, Stack, Text, Title } from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiKeyboardBackspace, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import GameNoticeEditCard from '@Components/admin/GameNoticeEditCard'
import GameNoticeEditModal from '@Components/admin/GameNoticeEditModal'
import WithGameTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { OnceSWRConfig } from '@Utils/useConfig'
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
      children: <Text> {t('admin.content.games.notices.delete')}</Text>,
      onConfirm: () => onConfirmDelete(gameNotice),
      confirmProps: { color: 'red' },
    })
  }
  const onConfirmDelete = (gameNotice: GameNotice) => {
    api.edit
      .editDeleteGameNotice(numId, gameNotice.id)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.notices.deleted'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate(gameNotices?.filter((t) => t.id !== gameNotice.id) ?? [])
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const navigate = useNavigate()
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

          <Group position="right">
            <Button
              leftIcon={<Icon path={mdiPlus} size={1} />}
              onClick={() => {
                setActiveGameNotice(null)
                setIsEditModalOpen(true)
              }}
            >
              {t('admin.button.notices.new')}
            </Button>
          </Group>
        </>
      }
    >
      <ScrollArea pos="relative" h="calc(100vh - 180px)" offsetScrollbars>
        {!gameNotices || gameNotices?.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack spacing={0}>
              <Title order={2}>{t('admin.content.games.notices.empty.title')}</Title>
              <Text>{t('admin.content.games.notices.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <Stack spacing="lg" align="center" m="2%">
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
    </WithGameTab>
  )
}

export default GameNoticeEdit
