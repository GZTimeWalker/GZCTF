import { Button, Group, Modal, ModalProps, Stack, Text, Textarea } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { showErrorMsg } from '@Utils/Shared'
import { useEditChallenge } from '@Hooks/useEdit'
import api from '@Api'
import misc from '@Styles/Misc.module.css'

export const FlagCreateModal: FC<ModalProps> = (props) => {
  const [disabled, setDisabled] = useState(false)

  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const [flags, setFlags] = useInputState('')

  const { challenge, mutate } = useEditChallenge(numId, numCId)

  const { t } = useTranslation()

  const onCreate = async () => {
    if (!flags) {
      return
    }

    const flagList = flags
      .split('\n')
      .filter((x) => x.trim().length > 0)
      .map((x) => ({ flag: x }))

    setDisabled(true)

    try {
      await api.edit.editAddFlags(numId, numCId, flagList)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.flag.created'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      if (challenge) {
        mutate({
          ...challenge,
          flags: [...(challenge.flags ?? []), ...flagList],
        })
      }
      setFlags('')
      props.onClose()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal {...props}>
      <Stack>
        <Text size="sm">
          <Trans i18nKey="admin.content.games.challenges.flag.create" />
        </Text>
        <Textarea
          w="100%"
          value={flags}
          disabled={disabled}
          autosize
          minRows={8}
          maxRows={8}
          onChange={setFlags}
          classNames={{
            input: misc.ffmono,
          }}
        />
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            {t('admin.button.challenges.flag.add.normal')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
