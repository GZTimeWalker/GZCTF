import React, { FC, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Stack, Button, Text, Group, ScrollArea } from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiAccountReactivate, mdiBackburger, mdiCheck, mdiClose, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import GameNoticeEditModal from '@Components/GameNoticeEditModal'
import WithGameTab from '@Components/admin/WithGameEditTab'
import api, { GameNotice } from '../../../../Api'
import GameNoticeEditCard from '../../../../components/GameNoticeEditCard'
import { showErrorNotification } from '../../../../utils/ApiErrorHandler'

const GameNoticeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { data: gameNotices, mutate } = api.edit.useEditGetGameNotices(numId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [disabled, setDisabled] = useState(false)
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeGameNotice, setActiveGameNotice] = useState<GameNotice | null>(null)

  // delete
  const modals = useModals()
  const onDeleteGameNotice = (gameNotice: GameNotice) => {
    if (disabled) {
      return
    }
    modals.openConfirmModal({
      title: '删除通知',
      children: <Text> 你确定要删除通知该通知吗？</Text>,
      onConfirm: () => onConfirmDelete(gameNotice),
      centered: true,
      labels: { confirm: '删除通知', cancel: '取消' },
      confirmProps: { color: 'red' },
    })
  }
  const onConfirmDelete = (gameNotice: GameNotice) => {
    api.edit
      .editDeleteGameNotice(numId, gameNotice.id!)
      .then(() => {
        showNotification({
          color: 'teal',
          message: '通知已删除',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        mutate(gameNotices?.filter((t) => t.id !== gameNotice.id) ?? [])
      })
      .catch(showErrorNotification)
  }

  const navigate = useNavigate()
  return (
    <WithGameTab
      headProps={{ position: 'apart' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            返回上级
          </Button>

          <Group position="center">
            <Button
              leftIcon={<Icon path={mdiPlus} size={1} />}
              onClick={() => {
                setActiveGameNotice(null)
                setIsEditModalOpen(true)
              }}
            >
              新建通知
            </Button>
          </Group>
        </>
      }
    >
      <ScrollArea style={{ height: 'calc(100vh-180px)', position: 'relative' }} offsetScrollbars>
        <Stack
          spacing="lg"
          align="center"
          style={{
            margin: '2%',
          }}
        >
          {gameNotices &&
            gameNotices.map((gameNotice) => (
              <GameNoticeEditCard
                key={gameNotice.id}
                gameNotice={gameNotice}
                onDelete={() => {
                  onDeleteGameNotice(gameNotice)
                }}
                onEdit={() => {
                  setActiveGameNotice(gameNotice)
                  setIsEditModalOpen(true)
                }}
                style={{ width: '90%' }}
              />
            ))}
        </Stack>
      </ScrollArea>
      <GameNoticeEditModal
        centered
        size="30%"
        title={activeGameNotice ? '编辑通知' : '新建通知'}
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
