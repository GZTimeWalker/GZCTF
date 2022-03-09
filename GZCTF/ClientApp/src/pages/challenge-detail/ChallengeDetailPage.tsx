import {
  Text,
  Box,
  Button,
  Center,
  Container,
  Flex,
  Heading,
  Input,
  useToast
} from '@chakra-ui/react';
import React, { FC, FormEvent, useCallback, useEffect, useState } from 'react';
import { LoadingMask } from 'src/common/components/LoadingMask';
import { resolveMessageForRateLimit } from 'src/common/utils';
import { Challenges_API } from 'src/redux/challenge.api';
import marked from 'marked';
import '../../common/utils/marked.css';

export interface ChallengesDetailPageProps {
  id: number;
}

export const ChallengesDetailPage: FC<ChallengesDetailPageProps> = ({ id }) => {
  const { isLoading, error, data } = Challenges_API.useGetChallengesQuery(id, { skip: Object.is(NaN, id) });
  const [submit, { isLoading: isAnswering, error: answerError, isSuccess: isAnswerSuccess }] =
    Challenges_API.useAnswerChallengesMutation();
  const [answer, setAnswer] = useState('');
  const toast = useToast();

  const onSubmit = useCallback(
    (event: FormEvent) => {
      event.preventDefault();
      if (answer) {
        submit([{ answer: answer.trim() }, id]);
      }
    },
    [answer, submit, id]
  );

  useEffect(() => {
    if (isAnswerSuccess) {
      toast({
        title: '回答正确',
        description: '恭喜你找到了这道题目的答案！',
        status: 'success',
        duration: 3000
      });
      setTimeout(() => {window.location.href = '/Challenges'}, 3000);
    }
  }, [isAnswerSuccess, toast]);

  useEffect(() => {
    if (error && error['data']['title'].includes('无权')) {
      window.location.href = '/404';
    }
  }, [error]);

  useEffect(() => {
    if(!isLoading)
    {
      let clientjs = document.createElement('script');
      clientjs.type = 'text/javascript';
      clientjs.id = 'clientjs';
      clientjs.text = data?.clientJS ?? "";
      document.getElementsByClassName('marked').item(0)?.appendChild(clientjs);
    }
    return (() => {
      document.getElementById('clientjs')?.remove();
    });
  },[isLoading, data]);

  if (Object.is(NaN, id)) {
    return (
      <Center h="100%">
        <Text>无效的题目 ID</Text>
      </Center>
    );
  }

  if (isLoading) {
    return <LoadingMask />;
  }

  if (error) {
    return <LoadingMask error={error} />;
  }

  return (
    <Container display="flex" flexDirection="column" height="100vh" maxWidth="50vw">
      <Flex flex="none" alignItems="center" mt="10vh">
        <Heading color="gray.300" size="2xl" textShadow="xl">
          # {data!.title}
        </Heading>
      </Flex>
      <Box flex="1" my="24px" overflow="auto">
        <div className="marked" dangerouslySetInnerHTML={{ __html: marked(data!.content) }}/>
      </Box>
      <Box flex="none" p="12px" roundedTopLeft="xl" roundedTopRight="xl" bg="gray.700">
        <Flex as="form" onSubmit={onSubmit}>
          <Input
            flex="1"
            mr="8px"
            placeholder="输入你的答案"
            value={answer}
            onChange={(event) => setAnswer(event.target.value)}
          />
          <Button
            flex="none"
            type="submit"
            disabled={!answer || isAnswering}
            isLoading={isAnswering}
          >
            {answerError && resolveMessageForRateLimit(answerError)}
            {!answerError && '提交'}
          </Button>
        </Flex>
      </Box>
    </Container>
  );
};
